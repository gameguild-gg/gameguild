using GameGuild.Enums;


using System.ComponentModel.DataAnnotations;
﻿namespace GameGuild.Learning.Courses;

/// <summary> DTO for individual content order items </summary>
public class ContentOrderDto {
  [Required] public Guid ContentId { get; set; }

  [Required] public int SortOrder { get; set; }
}
