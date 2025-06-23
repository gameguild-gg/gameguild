using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Test.Models
{
    public class TestingLocation : BaseEntity
    {
        private string _name = string.Empty;
        private string? _description;
        private string? _address;
        private int _maxTestersCapacity;
        private int _maxProjectsCapacity;
        private string? _equipmentAvailable;
        private LocationStatus _status = LocationStatus.Active;

        [Required, MaxLength(255)]
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        public string? Address
        {
            get => _address;
            set => _address = value;
        }

        [Required]
        public int MaxTestersCapacity
        {
            get => _maxTestersCapacity;
            set => _maxTestersCapacity = value;
        }

        [Required]
        public int MaxProjectsCapacity
        {
            get => _maxProjectsCapacity;
            set => _maxProjectsCapacity = value;
        }

        public string? EquipmentAvailable
        {
            get => _equipmentAvailable;
            set => _equipmentAvailable = value;
        }

        [Required]
        public LocationStatus Status
        {
            get => _status;
            set => _status = value;
        }
    }
}
