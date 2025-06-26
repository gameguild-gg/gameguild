using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.TestingLab.Models
{
    public class TestingParticipant : BaseEntity
    {
        private Guid _testingRequestId;

        private TestingRequest _testingRequest = null!;

        private Guid _userId;

        private User.Models.User _user = null!;

        private bool _instructionsAcknowledged = false;

        private DateTime? _instructionsAcknowledgedAt;

        private DateTime _startedAt = DateTime.UtcNow;

        private DateTime? _completedAt;

        /// <summary>
        /// Foreign key to the testing request
        /// </summary>
        public Guid TestingRequestId
        {
            get => _testingRequestId;
            set => _testingRequestId = value;
        }

        /// <summary>
        /// Navigation property to the testing request
        /// </summary>
        public virtual TestingRequest TestingRequest
        {
            get => _testingRequest;
            set => _testingRequest = value;
        }

        /// <summary>
        /// Foreign key to the user
        /// </summary>
        public Guid UserId
        {
            get => _userId;
            set => _userId = value;
        }

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public virtual Modules.User.Models.User User
        {
            get => _user;
            set => _user = value;
        }

        [Required]
        public bool InstructionsAcknowledged
        {
            get => _instructionsAcknowledged;
            set => _instructionsAcknowledged = value;
        }

        public DateTime? InstructionsAcknowledgedAt
        {
            get => _instructionsAcknowledgedAt;
            set => _instructionsAcknowledgedAt = value;
        }

        [Required]
        public DateTime StartedAt
        {
            get => _startedAt;
            set => _startedAt = value;
        }

        public DateTime? CompletedAt
        {
            get => _completedAt;
            set => _completedAt = value;
        }
    }
}
