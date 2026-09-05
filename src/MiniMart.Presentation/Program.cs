using MiniMart.Presentation.Common;
using MiniMart.Presentation.Forms;

namespace MiniMart.Presentation;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppServices services;
        try
        {
            services = AppServices.CreateFromConfiguration();
        }
        catch (Exception ex)
        {
            UiFeedback.ShowError(null, ex, "start the application");
            return;
        }

        while (true)
        {
            using var login = new LoginForm(services);

            if (login.ShowDialog() != DialogResult.OK || login.AuthenticatedUser is null)
            {
                return;
            }

            var user = login.AuthenticatedUser;

            // The signed-in user's role decides which main screen opens, as required by the
            // authentication feature: Admins reach the back-office dashboard, Cashiers the till.
            Form mainForm = user.IsAdmin
                ? new AdminDashboardForm(services, user)
                : new PosForm(services, user);

            using (mainForm)
            {
                Application.Run(mainForm);

                if (mainForm is not ILogoutAware { LogOutRequested: true })
                {
                    return;
                }
            }
        }
    }
}
