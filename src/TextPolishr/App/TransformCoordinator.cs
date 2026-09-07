using TextPolishr.Core;
using TextPolishr.UI;
using TextPolishr.Windows;

namespace TextPolishr.App;

internal sealed class TransformCoordinator : IDisposable
{
    private readonly AppSettings _settings;
    private readonly SelectionService _selection;
    private readonly ReliablePasteService _paste;
    private readonly LlmClient _llm;
    private readonly HistoryStore _history;
    private readonly OverlayForm _overlay;
    private readonly KeyboardHook _keyboardHook;
    private CancellationTokenSource? _operation;
    private ActionMenuForm? _actionMenu;
    private SelectionContext? _pendingSelection;

    public TransformCoordinator(
        AppSettings settings,
        LlmClient llm,
        HistoryStore history,
        OverlayForm overlay,
        KeyboardHook keyboardHook)
    {
        _settings = settings;
        _selection = new SelectionService();
        _paste = new ReliablePasteService();
        _llm = llm;
        _history = history;
        _overlay = overlay;
        _keyboardHook = keyboardHook;
        _keyboardHook.KeyDown = HandleGlobalKey;
    }

    public bool IsBusy => _operation is not null;
    public event EventHandler? BusyChanged;

    public async void OpenActionMenu() => await CaptureThenAsync(action: null);
    public async void RunAction(TransformAction action) => await CaptureThenAsync(action);

    public void Cancel()
    {
        if (_operation is null) return;
        var waitingForChoice = _actionMenu is not null;
        _operation.Cancel();
        CloseActionMenu();
        _overlay.ShowStatus("Cancelled · Nothing replaced", OverlayKind.Warning, 1800);
        if (waitingForChoice)
        {
            End();
        }
    }

    private async Task CaptureThenAsync(TransformAction? action)
    {
        if (!TryBegin())
        {
            _overlay.ShowStatus("Already processing · Press Esc to cancel", OverlayKind.Warning, 2200);
            return;
        }

        try
        {
            var selection = await _selection.CaptureAsync(_operation!.Token);
            if (selection is null)
            {
                var detail = string.IsNullOrWhiteSpace(_selection.LastFailure) ? string.Empty : $" · {_selection.LastFailure}";
                _overlay.ShowStatus($"No text selected{detail}", OverlayKind.Warning, 2600);
                End();
                return;
            }
            if (selection.SelectedText.Length > _settings.CharacterLimit)
            {
                _overlay.ShowStatus($"Selection too long · {_settings.CharacterLimit:N0} character limit", OverlayKind.Warning, 2600);
                End();
                return;
            }

            if (action is not null)
            {
                await ExecuteAsync(new TransformRequest(selection, action, null));
                return;
            }

            _pendingSelection = selection;
            _overlay.Hide();
            ShowActionMenu();
        }
        catch (OperationCanceledException)
        {
            End();
        }
        catch (Exception exception)
        {
            DiagnosticLog.Error("capture-selection", exception);
            _overlay.ShowStatus($"Could not capture selection · {exception.Message}", OverlayKind.Error, 3600);
            End();
        }
    }

    private void ShowActionMenu()
    {
        CloseActionMenu();
        _actionMenu = new ActionMenuForm(_settings.Actions);
        _actionMenu.ActionChosen += (_, action) =>
        {
            var selection = _pendingSelection;
            CloseActionMenu();
            if (selection is not null) _ = ExecuteAsync(new TransformRequest(selection, action, null));
        };
        _actionMenu.CustomChosen += (_, _) => ChooseCustomInstruction();
        _actionMenu.Cancelled += (_, _) =>
        {
            CloseActionMenu();
            End();
        };
        _actionMenu.ShowAtCursor();
    }

    private void ChooseCustomInstruction()
    {
        var selection = _pendingSelection;
        CloseActionMenu();
        if (selection is null)
        {
            End();
            return;
        }

        using var dialog = new CustomInstructionForm();
        if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Instruction))
        {
            End();
            return;
        }

        _selection.RestoreTarget(selection);
        var action = new TransformAction { Name = "Custom", Prompt = PromptTemplate.CustomPrompt, ShowInMenu = false };
        _ = ResumeCustomAsync(selection, action, dialog.Instruction);
    }

    private async Task ResumeCustomAsync(SelectionContext selection, TransformAction action, string instruction)
    {
        try
        {
            await Task.Delay(100, _operation!.Token);
            await ExecuteAsync(new TransformRequest(selection, action, instruction));
        }
        catch (OperationCanceledException)
        {
            End();
        }
    }

    private async Task ExecuteAsync(TransformRequest request)
    {
        try
        {
            var token = _operation!.Token;
            var providerId = string.IsNullOrWhiteSpace(request.Action.ProviderId)
                ? _settings.ActiveProviderId
                : request.Action.ProviderId;
            var provider = _settings.Providers.FirstOrDefault(item => item.Id == providerId)
                ?? throw new InvalidOperationException("Provider configuration not found.");
            var model = string.IsNullOrWhiteSpace(request.Action.Model)
                ? _settings.Models.GetValueOrDefault(provider.Id, string.Empty)
                : request.Action.Model;
            var apiKey = _settings.ApiKeys.GetValueOrDefault(provider.Id, string.Empty);
            var prompt = PromptTemplate.Render(request.Action.Prompt, request.Selection.SelectedText, request.Instruction);

            _overlay.ShowStatus($"{request.Action.Name}…", OverlayKind.Progress);
            var replacement = await _llm.TransformAsync(
                provider,
                apiKey,
                model ?? string.Empty,
                prompt,
                TimeSpan.FromSeconds(_settings.RequestTimeoutSeconds),
                token);

            _overlay.ShowStatus("Validating selection…", OverlayKind.Progress);
            if (!await _selection.ValidateAsync(request.Selection, token))
            {
                _history.Add(request.Action.Name, request.Selection.SelectedText, replacement);
                _overlay.ShowStatus("Selection changed · Nothing replaced", OverlayKind.Warning, 2800);
                return;
            }

            _overlay.ShowStatus("Replacing text…", OverlayKind.Progress);
            var confirmed = await _paste.PasteAsync(replacement);
            if (!confirmed)
            {
                _history.Add(request.Action.Name, request.Selection.SelectedText, replacement);
                _overlay.ShowStatus("Paste not confirmed · Nothing replaced", OverlayKind.Error, 2800);
                return;
            }

            _history.Add(request.Action.Name, request.Selection.SelectedText, replacement);
            _overlay.ShowStatus($"Text replaced · {request.Action.Name}", OverlayKind.Success, 1800);
        }
        catch (OperationCanceledException)
        {
            if (_operation is { IsCancellationRequested: false })
            {
                _overlay.ShowStatus("Request timed out · Original text unchanged", OverlayKind.Error, 2800);
            }
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("empty response", StringComparison.OrdinalIgnoreCase))
        {
            _overlay.ShowStatus("Empty response · Nothing replaced", OverlayKind.Warning, 2400);
        }
        catch (Exception exception)
        {
            var message = exception is HttpRequestException ? "Request failed" : exception.Message;
            _overlay.ShowStatus($"{message} · Nothing replaced", OverlayKind.Error, 3000);
        }
        finally
        {
            End();
        }
    }

    private bool HandleGlobalKey(Keys key)
    {
        if (_actionMenu is not null && _actionMenu.Visible)
        {
            return _actionMenu.HandleKey(key);
        }
        if (key == Keys.Escape && IsBusy)
        {
            _overlay.BeginInvoke(Cancel);
            return true;
        }
        return false;
    }

    private bool TryBegin()
    {
        if (_operation is not null) return false;
        _operation = new CancellationTokenSource();
        BusyChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void End()
    {
        _pendingSelection = null;
        _operation?.Dispose();
        _operation = null;
        BusyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CloseActionMenu()
    {
        if (_actionMenu is null) return;
        _actionMenu.Hide();
        _actionMenu.Dispose();
        _actionMenu = null;
    }

    public void Dispose()
    {
        CloseActionMenu();
        _operation?.Cancel();
        _operation?.Dispose();
        _paste.Dispose();
    }
}
