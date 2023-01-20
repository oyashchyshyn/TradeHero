using Telegram.Bot.Types.ReplyMarkups;
using TradeHero.Core.Types.Services;

// ReSharper disable ConvertToConstant.Global

namespace TradeHero.Application.Menu.Telegram.Store;

internal class TelegramMenuStore
{
    private readonly IStoreService _storeService;
    
    private readonly Dictionary<string, ReplyMarkupBase> _keyboards = new();
    private readonly List<string> _handleIncomeData = new();
    private readonly TelegramKeyboards _telegramKeyboards;
    
    public readonly TelegramButtonIds TelegramButtons;
    
    public readonly StrategyTempData StrategyData = new();
    public readonly CheckUpdateTempData CheckUpdateData = new();
    public string LastCommandId { get; set; } = string.Empty;
    public string GoBackCommandId { get; private set; } = string.Empty;

    public TelegramMenuStore(IStoreService storeService)
    {
        _storeService = storeService;
        
        TelegramButtons = new TelegramButtonIds();
        _telegramKeyboards = new TelegramKeyboards(TelegramButtons);
        
        SetButtonsHandleIncomeData();
        SetKeyboards();
    }

    public IEnumerable<string> ButtonsForHandleIncomeData()
    {
        return _handleIncomeData;
    }

    public ReplyMarkupBase GetKeyboard(string telegramMenuId)
    {
        if (telegramMenuId == TelegramButtons.Bot)
        {
            return _telegramKeyboards.GetBotKeyboard(_storeService.Bot.TradeLogic == null);
        }
        
        return _keyboards.ContainsKey(telegramMenuId) 
            ? _keyboards[telegramMenuId] 
            : new ReplyKeyboardMarkup(new List<KeyboardButton>());
    }
    
    public ReplyMarkupBase GetRemoveKeyboard()
    {
        return _telegramKeyboards.RemoveKeyboard();
    }
    
    public ReplyMarkupBase GetGoBackKeyboard(string whereToGo)
    {
        GoBackCommandId = whereToGo;
        
        return _telegramKeyboards.GetGoBackKeyboard();
    }
    
    public InlineKeyboardMarkup GetInlineKeyboard(IEnumerable<List<InlineKeyboardButton>> inlineKeyboardButtons)
    {
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    public void ClearData()
    {
        LastCommandId = string.Empty;
        GoBackCommandId = string.Empty;
        StrategyData.ClearData();
        CheckUpdateData.ClearData();
    }
    
    #region Private methods

    private void SetKeyboards()
    {
        _keyboards.Add(TelegramButtons.MainMenu, _telegramKeyboards.GetMainMenuKeyboard());
        _keyboards.Add(TelegramButtons.Positions, _telegramKeyboards.GetPositionsKeyboard());
        _keyboards.Add(TelegramButtons.Strategies, _telegramKeyboards.GetStrategiesKeyboard());
        _keyboards.Add(TelegramButtons.Connections, _telegramKeyboards.GetConnectionsKeyboard());
        _keyboards.Add(TelegramButtons.GoBackKeyboard, _telegramKeyboards.GetGoBackKeyboard());
    }

    private void SetButtonsHandleIncomeData()
    {
        _handleIncomeData.AddRange(new List<string>
        {
            TelegramButtons.About,
            TelegramButtons.StrategiesAdd,
            TelegramButtons.StrategiesUpdate,
            TelegramButtons.StrategiesSetActive,
            TelegramButtons.StrategiesDelete,
        });
    }
    
    #endregion

    private class TelegramKeyboards
    {
        private readonly TelegramButtonIds _telegramButtonIds;
        
        public TelegramKeyboards(
            TelegramButtonIds telegramButtonIds
            )
        {
            _telegramButtonIds = telegramButtonIds;
        }
        
        public ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            var keyboard = new List<List<KeyboardButton>>
            {
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.Bot}Bot"),
                    new KeyboardButton($"{_telegramButtonIds.Positions}Positions")
                },
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.Connections}Connections"),
                    new KeyboardButton($"{_telegramButtonIds.Strategies}Strategies")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }
        
        public ReplyKeyboardMarkup GetBotKeyboard(bool showStart)
        {
            var keyboard = new List<List<KeyboardButton>>
            {
                new()
                {
                    showStart 
                        ? new KeyboardButton($"{_telegramButtonIds.StartStrategy}Start strategy")
                        : new KeyboardButton($"{_telegramButtonIds.StopStrategy}Stop strategy"),
                    new KeyboardButton($"{_telegramButtonIds.CheckCodeStatus}Check code status")
                },
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.CheckUpdate}Check updates"),
                    new KeyboardButton($"{_telegramButtonIds.About}About")
                },
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.MainMenu}Main menu")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }

        public ReplyKeyboardMarkup GetStrategiesKeyboard()
        {
            var keyboard = new List<List<KeyboardButton>>
            {
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.StrategiesProperties}Show strategy properties"),
                    new KeyboardButton($"{_telegramButtonIds.StrategiesShow}Show strategies")
                },
                new() 
                {
                    new KeyboardButton($"{_telegramButtonIds.StrategiesSetActive}Set active"),
                    new KeyboardButton($"{_telegramButtonIds.StrategiesAdd}Add strategy")
                },
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.StrategiesUpdate}Update strategy"),
                    new KeyboardButton($"{_telegramButtonIds.StrategiesDelete}Delete strategy")
                },
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.MainMenu}Main menu")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }
        
        public ReplyKeyboardMarkup GetConnectionsKeyboard()
        {
            var keyboard = new List<List<KeyboardButton>>
            {
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.ConnectionsShow}Show connections"),
                    new KeyboardButton($"{_telegramButtonIds.ConnectionsSetActive}Set active")
                },
                new() 
                {
                    new KeyboardButton($"{_telegramButtonIds.ConnectionsAdd}Add connection"),
                    new KeyboardButton($"{_telegramButtonIds.ConnectionsDelete}Delete connection")
                },
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.MainMenu}Main menu")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }
        
        public ReplyKeyboardMarkup GetPositionsKeyboard()
        {
            var keyboard = new List<List<KeyboardButton>>
            {
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.WatchingPositions}Current opened positions"),
                    new KeyboardButton($"{_telegramButtonIds.MainMenu}Main menu")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }
        
        public ReplyKeyboardMarkup GetGoBackKeyboard()
        {
            var keyboard = new List<List<KeyboardButton>>
            {
                new()
                {
                    new KeyboardButton($"{_telegramButtonIds.GoBackKeyboard}Go back")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }

        public ReplyKeyboardRemove RemoveKeyboard()
        {
            return new ReplyKeyboardRemove();
        }
    }
    
    internal class TelegramButtonIds
    {
        public readonly string MainMenu = "\U000026EA ";

        public readonly string Bot = "\U0001F47E ";
        public readonly string StartStrategy = "\U0001F3C3 ";
        public readonly string StopStrategy = "\U0001F534 ";
        public readonly string CheckCodeStatus = "\U0001F4DF ";
        public readonly string CheckUpdate = "\U0000267B ";
        public readonly string About = "\U0001F491 ";
    
        public readonly string Positions = "\U0001F4BB ";
        public readonly string WatchingPositions = "\U0001F6BD ";
    
        public readonly string Strategies = "\U0001FA84 ";
        public readonly string StrategiesShow = $"\U0001FA84{Show} ";
        public readonly string StrategiesProperties = "\U0001FA84\U0001F7E4 ";
        public readonly string StrategiesAdd = $"\U0001FA84{Add} ";
        public readonly string StrategiesUpdate = $"\U0001FA84{Update} ";
        public readonly string StrategiesSetActive = $"\U0001FA84{SetActive} ";
        public readonly string StrategiesDelete = $"\U0001FA84{Delete} ";

        public readonly string Connections = "\U0001F4DE ";
        public readonly string ConnectionsShow = $"\U0001F4DE{Show} ";
        public readonly string ConnectionsAdd = $"\U0001F4DE{Add} ";
        public readonly string ConnectionsSetActive = $"\U0001F4DE{SetActive} ";
        public readonly string ConnectionsDelete = $"\U0001F4DE{Delete} ";

        public readonly string GoBackKeyboard = "\U0001F519 ";
        
        // System helpers
        private const string Show = "\U0001F535";
        private const string Add = "\U0001F7E2";
        private const string Update = "\U0001F7E1";
        private const string Delete = "\U0001F534";
        private const string SetActive = "\U0001F7E3";
    }
}