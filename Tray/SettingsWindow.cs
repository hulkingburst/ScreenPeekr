using ScreenPeekr.Models;
using ScreenPeekr.Services;

namespace ScreenPeekr.Tray;

internal sealed class SettingsWindow : Form
{
    private readonly ConfigStore _configStore;
    private readonly MonitorCatalog _monitorCatalog;
    private readonly StartupService _startup;
    private readonly EventLogStore _eventLog;

    private TextBox _webhookUrlTextBox = null!;
    private NumericUpDown _intervalNumeric = null!;
    private ComboBox _monitorComboBox = null!;
    private Button _keyBinderButton = null!;
    private NumericUpDown _inputDelayNumeric = null!;
    private CheckBox _startupCheckBox = null!;
    private CheckBox _enableChangeDetectionCheckBox = null!;
    private NumericUpDown _changeDetectionSensitivityNumeric = null!;
    private CheckBox _awayOnlyModeCheckBox = null!;
    private NumericUpDown _awayIdleThresholdNumeric = null!;
    private NumericUpDown _retentionDaysNumeric = null!;
    private NumericUpDown _retentionCountNumeric = null!;
    private Keys _selectedKey = Keys.None;

    public SettingsWindow(
        ConfigStore configStore,
        MonitorCatalog monitorCatalog,
        StartupService startup,
        EventLogStore eventLog)
    {
        _configStore = configStore;
        _monitorCatalog = monitorCatalog;
        _startup = startup;
        _eventLog = eventLog;

        InitializeComponent();
        LoadCurrentSettings();
    }

    private void InitializeComponent()
    {
        Text = "ScreenPeekr Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(500, 680);

        // Webhook URL
        var webhookLabel = new Label
        {
            Text = "Discord Webhook URL:",
            Left = 12,
            Top = 12,
            Width = 200,
            Height = 20
        };

        _webhookUrlTextBox = new TextBox
        {
            Left = 12,
            Top = 35,
            Width = 440,
            Height = 25
        };

        var webhookHelpButton = new Button
        {
            Text = "?",
            Left = 460,
            Top = 35,
            Width = 28,
            Height = 25
        };
        webhookHelpButton.Click += (_, _) => ShowWebhookInstructions();

        // Interval
        var intervalLabel = new Label
        {
            Text = "Capture Interval (seconds, min 5):",
            Left = 12,
            Top = 70,
            Width = 200,
            Height = 20
        };

        _intervalNumeric = new NumericUpDown
        {
            Left = 12,
            Top = 93,
            Width = 120,
            Height = 25,
            Minimum = 5,
            Maximum = 3600,
            Value = 60
        };

        // Monitor Selection
        var monitorLabel = new Label
        {
            Text = "Monitor:",
            Left = 12,
            Top = 128,
            Width = 200,
            Height = 20
        };

        _monitorComboBox = new ComboBox
        {
            Left = 12,
            Top = 151,
            Width = 476,
            Height = 25,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "DisplayName"
        };

        // Pre-Screenshot Input
        var inputLabel = new Label
        {
            Text = "Pre-Screenshot Key (None = disabled):",
            Left = 12,
            Top = 186,
            Width = 250,
            Height = 20
        };

        _keyBinderButton = new Button
        {
            Text = "Click to Bind Key",
            Left = 12,
            Top = 209,
            Width = 200,
            Height = 30
        };
        _keyBinderButton.Click += (_, _) => BindKey();

        // Input Delay
        var delayLabel = new Label
        {
            Text = "Input Delay (milliseconds):",
            Left = 12,
            Top = 249,
            Width = 200,
            Height = 20
        };

        _inputDelayNumeric = new NumericUpDown
        {
            Left = 12,
            Top = 272,
            Width = 120,
            Height = 25,
            Minimum = 0,
            Maximum = 5000,
            Value = 250
        };

        // Start with Windows
        _startupCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            Left = 12,
            Top = 307,
            Width = 200,
            Height = 24
        };

        // Change Detection
        var changeDetectionLabel = new Label
        {
            Text = "Change Detection:",
            Left = 12,
            Top = 341,
            Width = 200,
            Height = 20
        };

        _enableChangeDetectionCheckBox = new CheckBox
        {
            Text = "Enable Change Detection",
            Left = 12,
            Top = 364,
            Width = 200,
            Height = 24
        };

        var sensitivityLabel = new Label
        {
            Text = "Sensitivity (0=Very Sensitive, 100=Very Strict):",
            Left = 12,
            Top = 398,
            Width = 300,
            Height = 20
        };

        _changeDetectionSensitivityNumeric = new NumericUpDown
        {
            Left = 12,
            Top = 421,
            Width = 120,
            Height = 25,
            Minimum = 0,
            Maximum = 100,
            Value = 50
        };

        // Away Only Mode
        var awayOnlyLabel = new Label
        {
            Text = "Away-Only Mode:",
            Left = 12,
            Top = 456,
            Width = 200,
            Height = 20
        };

        _awayOnlyModeCheckBox = new CheckBox
        {
            Text = "Capture only when user is away",
            Left = 12,
            Top = 479,
            Width = 250,
            Height = 24
        };

        var awayThresholdLabel = new Label
        {
            Text = "Away Idle Threshold (seconds):",
            Left = 12,
            Top = 513,
            Width = 250,
            Height = 20
        };

        _awayIdleThresholdNumeric = new NumericUpDown
        {
            Left = 12,
            Top = 536,
            Width = 120,
            Height = 25,
            Minimum = 1,
            Maximum = 3600,
            Value = 300
        };

        // Screenshot Retention
        var retentionLabel = new Label
        {
            Text = "Screenshot Retention:",
            Left = 12,
            Top = 571,
            Width = 200,
            Height = 20
        };

        var retentionDaysLabel = new Label
        {
            Text = "Retention Days (0=unlimited):",
            Left = 12,
            Top = 594,
            Width = 200,
            Height = 20
        };

        _retentionDaysNumeric = new NumericUpDown
        {
            Left = 12,
            Top = 617,
            Width = 120,
            Height = 25,
            Minimum = 0,
            Maximum = 365,
            Value = 1
        };

        var retentionCountLabel = new Label
        {
            Text = "Max Screenshots to Keep (0=unlimited):",
            Left = 250,
            Top = 594,
            Width = 230,
            Height = 20
        };

        _retentionCountNumeric = new NumericUpDown
        {
            Left = 250,
            Top = 617,
            Width = 120,
            Height = 25,
            Minimum = 0,
            Maximum = 1000,
            Value = 50
        };

        // Buttons
        var saveButton = new Button
        {
            Text = "Save",
            Left = 320,
            Width = 80,
            Top = 640,
            DialogResult = DialogResult.OK
        };
        saveButton.Click += (_, _) => SaveSettings();

        var cancelButton = new Button
        {
            Text = "Cancel",
            Left = 408,
            Width = 80,
            Top = 640,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            webhookLabel, _webhookUrlTextBox, webhookHelpButton,
            intervalLabel, _intervalNumeric,
            monitorLabel, _monitorComboBox,
            inputLabel, _keyBinderButton,
            delayLabel, _inputDelayNumeric,
            _startupCheckBox,
            changeDetectionLabel, _enableChangeDetectionCheckBox,
            sensitivityLabel, _changeDetectionSensitivityNumeric,
            awayOnlyLabel, _awayOnlyModeCheckBox,
            awayThresholdLabel, _awayIdleThresholdNumeric,
            retentionLabel, retentionDaysLabel, _retentionDaysNumeric,
            retentionCountLabel, _retentionCountNumeric,
            saveButton, cancelButton
        });

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void ShowWebhookInstructions()
    {
        using var instructionsWindow = new WebhookInstructionsWindow();
        instructionsWindow.ShowDialog(this);
    }

    private void LoadCurrentSettings()
    {
        _webhookUrlTextBox.Text = _configStore.Config.WebhookUrl;
        _intervalNumeric.Value = _configStore.Config.IntervalSeconds;
        _selectedKey = _configStore.Config.PreScreenshotKeys.Count > 0 ? _configStore.Config.PreScreenshotKeys[0] : _configStore.Config.PreScreenshotKey;
        _keyBinderButton.Text = _selectedKey == Keys.None ? "Click to Bind Key" : $"Bound: {_selectedKey}";
        _inputDelayNumeric.Value = _configStore.Config.InputDelayMs;
        _startupCheckBox.Checked = _configStore.Config.StartWithWindows;
        _enableChangeDetectionCheckBox.Checked = _configStore.Config.EnableChangeDetection;
        _changeDetectionSensitivityNumeric.Value = _configStore.Config.ChangeDetectionSensitivity;
        _awayOnlyModeCheckBox.Checked = _configStore.Config.AwayOnlyMode;
        _awayIdleThresholdNumeric.Value = _configStore.Config.AwayIdleThresholdSeconds;
        _retentionDaysNumeric.Value = _configStore.Config.ScreenshotRetentionDays;
        _retentionCountNumeric.Value = _configStore.Config.ScreenshotRetentionCount;

        // Load monitors
        var monitors = new List<MonitorInfo>();
        try
        {
            monitors.Add(new MonitorInfo("ALL", "All Monitors", SystemInformation.VirtualScreen));
        }
        catch { }

        try
        {
            monitors.AddRange(_monitorCatalog.GetMonitors());
        }
        catch { }

        _monitorComboBox.Items.Clear();
        foreach (var monitor in monitors)
        {
            _monitorComboBox.Items.Add(monitor);
        }

        var selected = monitors.FirstOrDefault(m => string.Equals(m.Id, _configStore.Config.SelectedMonitor, StringComparison.OrdinalIgnoreCase));
        _monitorComboBox.SelectedItem = selected ?? monitors.FirstOrDefault();
    }

    private void BindKey()
    {
        using var binder = new KeyBinderForm(_selectedKey);
        if (binder.ShowDialog() == DialogResult.OK)
        {
            _selectedKey = binder.SelectedKey;
            _keyBinderButton.Text = _selectedKey == Keys.None ? "Click to Bind Key" : $"Bound: {_selectedKey}";
        }
    }

    private void SaveSettings()
    {
        var oldInterval = _configStore.Config.IntervalSeconds;
        var newInterval = (int)_intervalNumeric.Value;

        _configStore.Config.WebhookUrl = _webhookUrlTextBox.Text.Trim();
        _configStore.Config.IntervalSeconds = newInterval;
        _configStore.Config.PreScreenshotKey = _selectedKey;
        _configStore.Config.PreScreenshotKeys = _selectedKey != Keys.None ? new List<Keys> { _selectedKey } : new List<Keys>();
        _configStore.Config.InputDelayMs = (int)_inputDelayNumeric.Value;
        _configStore.Config.StartWithWindows = _startupCheckBox.Checked;
        _configStore.Config.EnableChangeDetection = _enableChangeDetectionCheckBox.Checked;
        _configStore.Config.ChangeDetectionSensitivity = (int)_changeDetectionSensitivityNumeric.Value;
        _configStore.Config.AwayOnlyMode = _awayOnlyModeCheckBox.Checked;
        _configStore.Config.AwayIdleThresholdSeconds = (int)_awayIdleThresholdNumeric.Value;
        _configStore.Config.ScreenshotRetentionDays = (int)_retentionDaysNumeric.Value;
        _configStore.Config.ScreenshotRetentionCount = (int)_retentionCountNumeric.Value;

        if (_monitorComboBox.SelectedItem is MonitorInfo selectedMonitor)
        {
            _configStore.Config.SelectedMonitor = selectedMonitor.Id;
        }

        _configStore.Save();
        _startup.SetStartWithWindows(_configStore.Config.StartWithWindows);

        _eventLog.Record("Settings saved");

        if (oldInterval != newInterval)
        {
            _eventLog.Record($"Interval changed: {newInterval} seconds");
        }
    }
}
