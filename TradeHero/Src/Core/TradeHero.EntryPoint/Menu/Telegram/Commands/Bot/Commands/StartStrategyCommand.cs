using Microsoft.Extensions.Logging;
using TradeHero.Contracts.Base.Enums;
using TradeHero.Contracts.Menu;
using TradeHero.Contracts.Repositories;
using TradeHero.Contracts.Services;
using TradeHero.Contracts.Store;
using TradeHero.Contracts.StrategyRunner;

namespace TradeHero.EntryPoint.Menu.Telegram.Commands.Bot.Commands;

internal class StartStrategyCommand : IMenuCommand
{
    private readonly ILogger<StartStrategyCommand> _logger;
    private readonly ITelegramService _telegramService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly ITradeLogicFactory _tradeLogicFactory;
    private readonly IStore _store;
    private readonly TelegramMenuStore _telegramMenuStore;

    public StartStrategyCommand(
        ILogger<StartStrategyCommand> logger,
        ITelegramService telegramService, 
        IStrategyRepository strategyRepository, 
        ITradeLogicFactory tradeLogicFactory, 
        IStore store, 
        TelegramMenuStore telegramMenuStore
        )
    {
        _logger = logger;
        _telegramService = telegramService;
        _strategyRepository = strategyRepository;
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
            
            var strategy = _tradeLogicFactory.GetTradeLogicRunner(activeStrategy.TradeLogicType);
            if (strategy == null)
            {
                await ErrorMessageAsync("Strategy does not exist.", cancellationToken);
            
                return;
            }

            await _telegramService.SendTextMessageToUserAsync(
                "In starting process...", 
                _telegramMenuStore.GetRemoveKeyboard(),
                cancellationToken: cancellationToken
            );
        
            var strategyResult = await strategy.InitAsync(activeStrategy);
            if (strategyResult != ActionResult.Success)
            {
                await strategy.FinishAsync(true);
                
                await ErrorMessageAsync($"Cannot start '{activeStrategy.Name}' strategy. Error code: {strategyResult}", cancellationToken);
            
                return;
            }
        
            _store.Bot.SetTradeLogic(strategy, TradeLogicStatus.Running);
        
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