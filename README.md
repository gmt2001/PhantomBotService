# PhantomBotService
Runs phantomBot as a service on Windows using .Net 4.7.2

This requires .NET Framework 4.7.2, setup.exe should automatically install this for you.

NOTE: You need BOTH setup.exe and PhantomBotServiceSetup.msi to perform the install. If you install .NET Framework 4.7.2 manually (or already have it), then you should be okay with just PhantomBotServiceSetup.msi

To use the service:

    Install PhantomBotService wherever you like using setup.exe, it may be easiest to set the install location to the same
    folder as PhantomBot. NOTE: If setup.exe successfully installs .NET Framework 4.7.2, it will automatically run
    PhantomBotServiceSetup.msi for you
    In the folder where you installed PhantomBotService, edit PhantomBotService.config in a text editor, such as Notepad or
    Notepad++, and ensure that the path on the line underneath [Bot Install Directory] is set to the same folder as your
    PhantomBot.jar file. Optionally set some of the other directives, listed at the bottom of the release notes
    Start the service from Microsoft Management Console. The easiest way to get there is to right-click the Start button,
    click Computer Management and then go to Computer Management > Services and Applications > Services on the left pane

The logging feature will save a log file in the same folder as PhantomBot.jar containing the output of the bot console. The file name will be unique each time the server starts the bot

The service will attempt to restart the bot if you try to use any method to stop it other than stopping the service through Microsoft Management Console or shutting down the computer

If the service encounters an Exception at the service level while attempting to start the bot, such as File Access Errors, it will be logged in Windows Event Viewer on the Microsoft Management Console. Right-click the Start button, click Computer Management and then go to Computer Management > System Tools > Event Viewer > Windows Logs > Application on the left pane. Look for entries with a Source of PhantomBotService

If setup.exe fails to install .NET Framework 4.7.2, follow these steps

    Download the offline installer for .NET Framework 4.7.2
        https://support.microsoft.com/en-us/help/4054530/microsoft-net-framework-4-7-2-offline-installer-for-windows
    Install .NET Framework 4.7.2 from the offline installer
    Run PhantomBotServiceSetup.msi to install the service

Available options in PhantomBotService.config:

    [Bot Install Directory] - (Required) The line under this directive must be the full path to the bot installation directory
        (where PhantomBot.jar is located)
    [Logging Enabled] - (Optional, Defaults to false) The line under this directive can be changed to "true" (without quotes)
        to enable a log of the bot console output to be created in the bot installation directory
    [Launch Command] - (Optional, Defaults to the java included with the bot) The line under this directive can be used to
        override the command used to launch the bot
    [Launch Arguments] - (Optional, Defaults to the arguments used by launch.bat) The line under this directive can be used
        to override the command line arguments provided to the launch command
