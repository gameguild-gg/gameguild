using GameGuild.SharedKernel.Enums;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Programs.Entities;
using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Modules.Programs;

/// <summary> DTO for individual content order items </summary>
public class ContentOrderDto {
  [Required] public Guid ContentId { get; set; }

  [Required] public int SortOrder { get; set; }
}
