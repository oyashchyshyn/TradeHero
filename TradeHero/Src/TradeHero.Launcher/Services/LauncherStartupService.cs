using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TradeHero.Core.Constants;
using TradeHero.Core.Enums;
using TradeHero.Core.Helpers;
using TradeHero.Core.Types.Repositories;
using TradeHero.Core.Types.Repositories.Models;
using TradeHero.Core.Types.Services;

namespace TradeHero.Launcher.Services;

internal class LauncherStartupService
{
    private readonly ILogger<LauncherStartupService> _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly ITerminalService _terminalService;
    private readonly IGithubService _githubService;
    private readonly IUserRepository _userRepository;

    private Process? _runningProcess;
    private bool _isNeedToUpdateApp;
    private readonly ManualResetEvent _waitAppClosed = new(false);
    
    public readonly ManualResetEvent AppWaiting = new(false);

    public LauncherStartupService(
        ILogger<LauncherStartupService> logger,
        IEnvironmentService environmentService,
        ITerminalService terminalService,
        IGithubService githubService,
        IUserRepository userRepository
        )
    {
        _logger = logger;
        _environmentService = environmentService;
        _terminalService = terminalService;
        _githubService = githubService;
        _userRepository = userRepository;
    }

    public void Start()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
        
        _logger.LogInformation("Started. Press Ctrl+C to shut down");
        _logger.LogInformation("Process id: {ProcessId}", _environmentService.GetCurrentProcessId());
        _logger.LogInformation("Base path: {GetBasePath}", _environmentService.GetBasePath());
        _logger.LogInformation("Environment: {GetEnvironmentType}", _environmentService.GetEnvironmentType());
        _logger.LogInformation("Runner type: {RunnerType}", RunnerType.Launcher);
    }

    public async Task<bool> ManageDatabaseDataAsync()
    {
        try
        {
            _terminalService.ClearConsole();

            var activeUser = await _userRepository.GetActiveUserAsync();
            if (activeUser != null)
            {
                return true;
            }

            var errorMessage = string.Empty;
        
            while (true)
            {
                int userTelegramId;
                while (true)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        _terminalService.WriteLine(errorMessage, ConsoleColor.Red);
                        _terminalService.WriteLine(string.Empty);
                    }
                
                    _terminalService.WriteLine("Write down your telegram id:");
                    var telegramIdString = _terminalService.ReadLine();
                
                    if (string.IsNullOrWhiteSpace(telegramIdString))
                    {
                        errorMessage = "User telegram id cannot be empty.";
                        _terminalService.ClearConsole();
                        continue;
                    }

                    if (!int.TryParse(telegramIdString, out userTelegramId))
                    {
                        errorMessage = "Cannot read user telegram id.";
                        _terminalService.ClearConsole();
                        continue;
                    }
                
                    if (userTelegramId == 0)
                    {
                        errorMessage = "User telegram id cannot be zero.";
                        _terminalService.ClearConsole();
                        continue;
                    }
                
                    break;
                }
            
                errorMessage = string.Empty;
                _terminalService.ClearConsole();
            
                TelegramBotClient telegramClient;
                string? botTelegramApiKey;
                while (true)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        _terminalService.WriteLine(errorMessage, ConsoleColor.Red);
                        _terminalService.WriteLine(string.Empty);
                    }
                
                    _terminalService.WriteLine("Write down bot telegram api key:");
                    botTelegramApiKey = _terminalService.ReadLine();
            
                    if (string.IsNullOrWhiteSpace(botTelegramApiKey))
                    {
                        errorMessage = "Telegram api key cannot be empty.";
                        _terminalService.ClearConsole();
                        continue;
                    }
                
                    try
                    {
                        telegramClient = new TelegramBotClient(botTelegramApiKey);
                        if (!await telegramClient.TestApiAsync())
                        {
                            errorMessage = "Cannot connect to bot by this api key.";
                            _terminalService.ClearConsole();
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        errorMessage = "Cannot connect to bot by this api key.";
                        _terminalService.ClearConsole();
                        continue;
                    }

                    break;
                }

                errorMessage = string.Empty;
                _terminalService.ClearConsole();

                var isError = false;
                try
                {
                    var getUserChat = await telegramClient.GetChatAsync(userTelegramId);
                    if (getUserChat.Id == 0)
                    {
                        isError = true;
                    }
                }
                catch (Exception)
                {
                    isError = true;
                }

                if (isError)
                {
                    errorMessage = 
                        $"Cannot get chat with user.{Environment.NewLine}" +
                        $"Please, be attentive when writing data.{Environment.NewLine}" +
                        $"Also, make sure that user send '/start' command or send a message to bot.{Environment.NewLine}" +
                        "Make sure that you solve problems above and insert data one more time.";
                    _terminalService.ClearConsole();
                    continue;
                }

                string? userName;
                while (true)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        _terminalService.WriteLine(errorMessage, ConsoleColor.Red);
                        _terminalService.WriteLine(string.Empty);
                    }
                
                    _terminalService.WriteLine("Write down name for current data (Minimum length 3 symbols, Maximum length 40 symbols, Do not contain spaces):");
                    userName = _terminalService.ReadLine();
            
                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        errorMessage = "Name cannot be empty.";
                        _terminalService.ClearConsole();
                        continue;
                    }
                
                    if (userName.Contains(" "))
                    {
                        errorMessage = "Name contains spaces.";
                        _terminalService.ClearConsole();
                        continue;
                    }

                    switch (userName.Length)
                    {
                        case < 3:
                            errorMessage = $"Minimum length 3. Your length {userName.Length}.";
                            _terminalService.ClearConsole();
                            continue;
                        case > 40:
                            errorMessage = $"Maximum length 40. Your length {userName.Length}.";
                            _terminalService.ClearConsole();
                            continue;
                    }

                    break;
                }

                _terminalService.ClearConsole();

                var createdUser = await _userRepository.AddUserAsync(new UserDto
                {
                    Name = userName,
                    TelegramBotToken = botTelegramApiKey,
                    TelegramUserId = userTelegramId
                });

                if (createdUser == null)
                {
                    return false;
                }

                var setUserActiveResult = await _userRepository.SetUserActiveAsync(createdUser.Id);
                
                return setUserActiveResult;
            }
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ManageDatabaseDataAsync));

            return false;
        }
    }

    public void RunApp()
    {
        Task.Run(async () =>
        {
            var appSettings = _environmentService.GetAppSettings();
            var appPath = Path.Combine(_environmentService.GetBasePath(), _environmentService.GetRunningApplicationName());
            var releaseAppPath = Path.Combine(_environmentService.GetBasePath(), _environmentService.GetReleaseApplicationName());

            while (true)
            {
                if (!File.Exists(appPath))
                {
                    _terminalService.WriteLine("Preparing application...");

                    var latestRelease = await _githubService.GetLatestReleaseAsync();
                    if (latestRelease.ActionResult != ActionResult.Success)
                    {
                        throw new Exception("Cannot find remote additional data for bot.");
                    }
                    
                    var downloadResult = await _githubService.DownloadReleaseAsync(latestRelease.Data.AppDownloadUri, appPath);
                    if (downloadResult.ActionResult != ActionResult.Success)
                    {
                        throw new Exception("Cannot download remote additional data for bot.");
                    }
                    
                    _terminalService.ClearConsole();
                }

                var arguments = $"{ArgumentKeyConstants.Environment}{_environmentService.GetEnvironmentType()} " +
                        $"{appSettings.Application.RunAppKey}";

                if (_isNeedToUpdateApp)
                {
                    File.Move(releaseAppPath, appPath, true);

                    arguments += $" {ArgumentKeyConstants.Update}";

                    _isNeedToUpdateApp = false;
                }
            
                if (_environmentService.GetCurrentOperationSystem() == OperationSystem.Linux)
                {
                    EnvironmentHelper.SetFullPermissionsToFileLinux(appPath);
                }
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = arguments,
                    UseShellExecute = false
                };

                _runningProcess = Process.Start(processStartInfo);
                if (_runningProcess == null)
                {
                    _logger.LogWarning("App process did not started! In {Method}", nameof(RunApp));

                    return;
                }
                
                _logger.LogInformation("App process started! In {Method}", nameof(RunApp));

                await _runningProcess.WaitForExitAsync();

                _logger.LogInformation("App stopped. Exit code: {ExitCode}. In {Method}", 
                    _runningProcess.ExitCode, nameof(RunApp));
                
                if (_runningProcess.ExitCode == (int)AppExitCode.Update)
                {
                    _isNeedToUpdateApp = true;
                    
                    _runningProcess?.Dispose();
                    _runningProcess = null;
                    
                    _logger.LogInformation("App is going to be updated. In {Method}", nameof(RunApp));
                    
                    continue;
                }

                _runningProcess?.Dispose();
                _runningProcess = null;

                _waitAppClosed.Set();

                break;
            }
        });
    }
    
    public void Finish()
    {
        AppDomain.CurrentDomain.UnhandledException -= UnhandledException;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Console.CancelKeyPress -= OnCancelKeyPress;

        _logger.LogInformation("Finish disposing. In {Method}", nameof(Finish));
    }
    
    #region Private methods

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        _logger.LogInformation("Ctrl + C is pressed. In {Method}", nameof(OnCancelKeyPress));
        
        e.Cancel = true;

        _waitAppClosed.WaitOne();
        
        Environment.ExitCode = (int)AppExitCode.Success;
        
        AppWaiting.Set();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit button is pressed. In {Method}", nameof(OnCancelKeyPress));

        _runningProcess?.Close();
        
        _waitAppClosed.WaitOne();
        
        Environment.ExitCode = (int)AppExitCode.Success;
        
        AppWaiting.Set();
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();

            _logger.LogError("Error in {Method}. Message: {Message}", nameof(UnhandledException), 
                $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}");
        }
        catch (Exception currentException)
        {
            _logger.LogError(currentException, "Error in {Method}", nameof(UnhandledException));
        }
        finally
        {
            _logger.LogError((Exception)e.ExceptionObject, 
                "Error in {Method}. Message: Unhandled exception (AppDomain.CurrentDomain.UnhandledException)", 
                nameof(UnhandledException));
        }
    }

    #endregion
}