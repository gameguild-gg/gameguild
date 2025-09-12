using System.ComponentModel;


namespace GameGuild.Common;

public enum ProductAcquisitionType {
  [Description("Product acquired through direct payment")]
  Purchase,

  [Description("Product access via recurring subscription")]
  Subscription,

  [Description("Product provided at no cost")]
  Free,

  [Description("Product received as a gift from another user")]
  Gift,
}
