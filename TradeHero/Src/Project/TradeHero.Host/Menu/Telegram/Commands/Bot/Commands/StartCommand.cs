using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Menu.Commands;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Trading;
using TradeHero.Core.Enums;
using TradeHero.Host.Menu.Telegram.Store;

namespace TradeHero.Host.Menu.Telegram.Commands.Bot.Commands;

internal class StartCommand : ITelegramMenuCommand
{
    private readonly ILogger<StartCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IConnectionRepository _connectionRepository;
    private readonly ITradeLogicFactory _tradeLogicFactory;
    private readonly IStore _store;
    private readonly TelegramMenuStore _telegramMenuStore;

    public StartCommand(
        ILogger<StartCommand> logger,
        ITelegramService telegramService, 
        IStrategyRepository strategyRepository, 
        IConnectionRepository connectionRepository,
        ITradeLogicFactory tradeLogicFactory, 
        IStore store, 
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _strategyRepository = strategyRepository;
        _connectionRepository = connectionRepository;
        _tradeLogicFactory = tradeLogicFactory;
        _store = store;
        _telegramMenuStore = telegramMenuStore;
    }
    
    public string Id => _telegramMenuStore.TelegramButtons.StartStrategy;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _telegramMenuStore.LastCommandId = Id;
        
            var activeStrategy = await _strategyRepository.GetActiveStrategyAsync();
            if (activeStrategy == null)
            {
                await ErrorMessageAsync("There is no active strategy.", cancellationToken);
                
                return;
            }
            
            var connection = await _connectionRepository.GetActiveConnectionAsync();
            if (connection == null)
            {
                await ErrorMessageAsync("There is no active connection to exchanger.", cancellationToken);
                
                return;
            }
            
            var tradeLogic = _tradeLogicFactory.GetTradeLogicRunner(activeStrategy.TradeLogicType);
            if (tradeLogic == null)
            {
                await ErrorMessageAsync("Strategy does not exist.", cancellationToken);
            
                return;
            }

            await _telegramService.SendTextMessageToUserAsync(
                "In starting process...", 
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );
        
            var strategyResult = await tradeLogic.InitAsync(activeStrategy);
            if (strategyResult != ActionResult.Success)
            {
                await tradeLogic.FinishAsync(true);
                
                await ErrorMessageAsync($"Cannot start '{activeStrategy.Name}' strategy. Error code: {strategyResult}", cancellationToken);
            
                return;
            }
        
            _store.Bot.SetTradeLogic(tradeLogic, TradeLogicStatus.Running);
        
            await _telegramService.SendTextMessageToUserAsync(
                "Strategy started! Enjoy lazy pidor.", 
                _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "In {Method}", nameof(ExecuteAsync));

            await ErrorMessageAsync("There was an error during process, please, try later.", cancellationToken);
        }
    }

    public Task HandleIncomeDataAsync(string data, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public Task HandleCallbackDataAsync(string callbackData, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    #region Private methods
    
    private async Task ErrorMessageAsync(string message, CancellationToken cancellationToken)
    {
        _telegramMenuStore.ClearData();
        
        await _telegramService.SendTextMessageToUserAsync(
            message,
            _telegramMenuStore.GetRemoveKeyboard(),
            cancellationToken: cancellationToken
        );
        
        await _telegramService.SendTextMessageToUserAsync(
            "Choose action:",
            _telegramMenuStore.GetKeyboard(_telegramMenuStore.TelegramButtons.Bot),
            cancellationToken: cancellationToken
        );
    }

    #endregion
}