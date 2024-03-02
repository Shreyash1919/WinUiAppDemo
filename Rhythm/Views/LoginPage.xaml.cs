using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Oracle.ManagedDataAccess.Client;
using Rhythm.Contracts.Services;
using Rhythm.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rhythm.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        this.InitializeComponent();

        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = "Rhythm - Login";
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(RequestedTheme);
        ValidateDetails();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        App.AppTitlebar = AppTitleBarText as UIElement;
    }

    private void ValidateDetails()
    {
        if (string.IsNullOrEmpty(Username.Text) || string.IsNullOrEmpty(Password.Password))
        {
            LoginButton.IsEnabled = false;
        }
        else
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void Username_TextChanged(object sender, TextChangedEventArgs e)
    {

        ValidateDetails();
    }

    private void Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
        ValidateDetails();
    }

    private string? Login(string username, string password)
    {
        var connection = App.GetService<IDatabaseService>().GetOracleConnection();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM USERS WHERE USERNAME = :username";
        command.Parameters.Add(new OracleParameter("username", username));
        var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var storedPassword = reader.GetString(reader.GetOrdinal("PASSWORD"));
            if (BCrypt.Net.BCrypt.Verify(password, storedPassword))
            {
                return reader.GetString(reader.GetOrdinal("USER_ID"));
            }
        }
        return null;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginButtonProgressRing.IsActive = true;
        LoginButtonProgressRing.Visibility = Visibility.Visible;
        LoginButtonProgressRing.Margin = new Thickness(0, 0, 10, 0);
        LoginButton.IsEnabled = false;
        var username = Username.Text;
        var password = Password.Password;
        var rememberMe = RememberMe.IsChecked;
        var result = await Task.Run(() => Login(username, password));
        if (result is not null && rememberMe == true)
        {
            await App.GetService<ILocalSettingsService>().SaveSettingAsync("IsAuthenticated", true);
        }
        if (result is not null)
        {
            await App.GetService<ILocalSettingsService>().SaveSettingAsync("UserId", result);
            App.MainWindow.Content = App.GetService<ShellPage>();
        }
        else
        {
            await App.MainWindow.ShowMessageDialogAsync("Invalid username or password", "Login Failed");
        }
        LoginButtonProgressRing.IsActive = false;
        LoginButtonProgressRing.Visibility = Visibility.Collapsed;
        LoginButtonProgressRing.Margin = new Thickness(0, 0, 0, 0);
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        App.MainWindow.Content = App.GetService<RegisterPage>();
    }
}
