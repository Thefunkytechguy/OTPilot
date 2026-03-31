using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OTPilot.Models;
using OTPilot.Services;

namespace OTPilot.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    private readonly TotpAccount _account;
    private readonly TotpService _totpService;

    // Accent colours cycle through this palette per account index
    public static readonly string[] AccentPalette =
    {
        "#2563EB", // blue
        "#DC2626", // red
        "#16A34A", // green
        "#D97706", // amber
        "#7C3AED", // purple
        "#0891B2", // cyan
        "#DB2777", // pink
        "#EA580C", // orange
    };

    [ObservableProperty]
    private string _currentCode = string.Empty;

    [ObservableProperty]
    private int _secondsRemaining;

    [ObservableProperty]
    private double _progress; // 0–100

    [ObservableProperty]
    private bool _isCodeVisible = true;

    public string Id => _account.Id;
    public string Issuer => string.IsNullOrWhiteSpace(_account.Issuer) ? _account.AccountName : _account.Issuer;
    public string AccountName => _account.AccountName;
    public string AccentColor { get; }
    public SolidColorBrush AccentBrush { get; }

    public string FormattedCode
    {
        get
        {
            if (string.IsNullOrEmpty(CurrentCode)) return string.Empty;

            return CurrentCode.Length switch
            {
                6 => $"{CurrentCode[..3]} {CurrentCode[3..]}",
                8 => $"{CurrentCode[..4]} {CurrentCode[4..]}",
                _ => CurrentCode
            };
        }
    }

    public AccountViewModel(TotpAccount account, TotpService totpService, int index = 0)
    {
        _account = account;
        _totpService = totpService;
        AccentColor = AccentPalette[index % AccentPalette.Length];
        AccentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(AccentColor));
        Refresh();
    }

    public void Refresh()
    {
        var newCode = _totpService.GenerateCode(_account);
        if (newCode != CurrentCode)
        {
            CurrentCode = newCode;
            OnPropertyChanged(nameof(FormattedCode));
        }

        SecondsRemaining = _totpService.GetSecondsRemaining(_account.Period);
        Progress = (double)SecondsRemaining / _account.Period * 100.0;
    }

    public TotpAccount ToModel() => _account;
}
