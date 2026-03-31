using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OTPilot.Models;
using OTPilot.Services;

namespace OTPilot.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly VaultService _vaultService;
    private readonly TotpService _totpService;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private ObservableCollection<AccountViewModel> _accounts = new();

    [ObservableProperty]
    private double _globalProgress = 100;

    [ObservableProperty]
    private int _globalSecondsRemaining = 30;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasAccounts => Accounts.Count > 0;

    public MainViewModel(VaultService vaultService, TotpService totpService)
    {
        _vaultService = vaultService;
        _totpService = totpService;

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _timer.Tick += OnTimerTick;
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        await _vaultService.LoadAsync();

        int index = 0;
        foreach (var account in _vaultService.Accounts)
            Accounts.Add(new AccountViewModel(account, _totpService, index++));

        OnPropertyChanged(nameof(HasAccounts));
        IsLoading = false;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var secs = _totpService.GetSecondsRemaining(30);
        GlobalSecondsRemaining = secs;
        GlobalProgress = secs / 30.0 * 100.0;

        foreach (var vm in Accounts)
            vm.Refresh();
    }

    public async Task AddAccountAsync(TotpAccount account)
    {
        await _vaultService.AddAccountAsync(account);
        var vm = new AccountViewModel(account, _totpService, Accounts.Count);
        Accounts.Add(vm);
        OnPropertyChanged(nameof(HasAccounts));
    }

    [RelayCommand]
    public async Task DeleteAccountAsync(AccountViewModel account)
    {
        await _vaultService.RemoveAccountAsync(account.Id);
        Accounts.Remove(account);
        OnPropertyChanged(nameof(HasAccounts));
    }
}
