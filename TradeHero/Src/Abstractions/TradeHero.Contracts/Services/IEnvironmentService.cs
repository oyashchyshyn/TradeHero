﻿using TradeHero.Contracts.Services.Models.Environment;
using TradeHero.Core.Enums;

namespace TradeHero.Contracts.Services;

public interface IEnvironmentService
{
    Dictionary<string, string> CustomArgs { get; }
    string[] GetEnvironmentArgs();
    EnvironmentSettings GetEnvironmentSettings();
    Version GetCurrentApplicationVersion();
    string GetBasePath();
    string GetDataFolderPath();
    string GetLogsFolderPath();
    string GetDatabaseFolderPath();
    string GetUpdateFolderPath();
    EnvironmentType GetEnvironmentType();
    int GetCurrentProcessId();
    OperationSystem GetCurrentOperationSystem();
    string GetCurrentApplicationName();
}