using Microsoft.Extensions.Logging;
using Telegram.Bot;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Repositories.Models;
using TradeHero.Contracts.Services;
using TradeHero.Core.Enums;
using TradeHero.Core.Exceptions;

namespace TradeHero.Services.Services;

internal class StartupService : IStartupService
{
    private readonly ILogger<StartupService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ITerminalService _terminalService;

    public StartupService(
        ILogger<StartupService> logger,
        IUserRepository userRepository, 
        ITerminalService terminalService
        )
    {
        _logger = logger;
        _userRepository = userRepository;
        _terminalService = terminalService;
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
}