using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace OTPilot.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var asm  = Assembly.GetExecutingAssembly();
        var ver  = asm.GetName().Version;

        VersionText.Text   = $"Version {ver?.Major}.{ver?.Minor}.{ver?.Build}";
        GithubText.Text    = "github.com/Thefunkytechguy/OTPilot";
        FollowText.Text    = "github.com/Thefunkytechguy";
        CopyrightText.Text = $"© {DateTime.Now.Year} Eugene Myburgh";
    }

    private void GitHub_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName        = "https://github.com/Thefunkytechguy/OTPilot",
            UseShellExecute = true
        });
    }

    private void Follow_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName        = "https://github.com/Thefunkytechguy",
            UseShellExecute = true
        });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
