using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

public record CreateProductFromProgramDto(string Name, string? Description, decimal BasePrice, string Currency = "USD") {
  public string Name { get; init; } = Name;

  public string? Description { get; init; } = Description;

  public decimal BasePrice { get; init; } = BasePrice;

  public string Currency { get; init; } = Currency;
}
