using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ShareCad
{
    // bliver ikke brugt til noget.
    internal static class ModuleInitializer
    {
        internal static void Run()
        {

        }
    }
}

/*
var messageBoxResult = MessageBox.Show("Host?", 
    "ShareCad", 
    MessageBoxButton.YesNoCancel,
    MessageBoxImage.Question, 
    MessageBoxResult.Cancel, 
    MessageBoxOptions.DefaultDesktopOnly
    );

switch (messageBoxResult)
{
    case MessageBoxResult.Yes:
        // stuff
        break;

    case MessageBoxResult.No:
        // stuff
        break;

    default:
        break;
}*/