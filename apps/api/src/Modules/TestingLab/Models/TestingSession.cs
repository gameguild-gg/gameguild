using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;
using GameGuild.Modules.User.Models;

namespace GameGuild.Modules.TestingLab.Models
{
    public class TestingSession : BaseEntity
    {
        private Guid _testingRequestId;

        private TestingRequest _testingRequest = null!;

        private Guid _locationId;

        private TestingLocation _location = null!;

        private string _sessionName = string.Empty;

        private DateTime _sessionDate;

        private DateTime _startTime;

        private DateTime _endTime;

        private int _maxTesters;

        private int _registeredTesterCount = 0;

        private int _registeredProjectMemberCount = 0;

        private int _registeredProjectCount = 0;

        private SessionStatus _status = SessionStatus.Scheduled;

        private Guid _managerUserId;

        private User.Models.User _manager = null!;

        private Guid _createdById;

        private User.Models.User _createdBy = null!;

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
        /// Foreign key to the testing location
        /// </summary>
        public Guid LocationId
        {
            get => _locationId;
            set => _locationId = value;
        }

        /// <summary>
        /// Navigation property to the testing location
        /// </summary>
        public virtual TestingLocation Location
        {
            get => _location;
            set => _location = value;
        }

        [Required, MaxLength(255)]
        public string SessionName
        {
            get => _sessionName;
            set => _sessionName = value;
        }

        [Required]
        public DateTime SessionDate
        {
            get => _sessionDate;
            set => _sessionDate = value;
        }

        [Required]
        public DateTime StartTime
        {
            get => _startTime;
            set => _startTime = value;
        }

        [Required]
        public DateTime EndTime
        {
            get => _endTime;
            set => _endTime = value;
        }

        [Required]
        public int MaxTesters
        {
            get => _maxTesters;
            set => _maxTesters = value;
        }

        public int RegisteredTesterCount
        {
            get => _registeredTesterCount;
            set => _registeredTesterCount = value;
        }

        public int RegisteredProjectMemberCount
        {
            get => _registeredProjectMemberCount;
            set => _registeredProjectMemberCount = value;
        }

        public int RegisteredProjectCount
        {
            get => _registeredProjectCount;
            set => _registeredProjectCount = value;
        }

        [Required]
        public SessionStatus Status
        {
            get => _status;
            set => _status = value;
        }

        /// <summary>
        /// Foreign key to the session manager
        /// </summary>
        public Guid ManagerUserId
        {
            get => _managerUserId;
            set => _managerUserId = value;
        }

        /// <summary>
        /// Navigation property to the session manager
        /// </summary>
        public virtual Modules.User.Models.User Manager
        {
            get => _manager;
            set => _manager = value;
        }

        /// <summary>
        /// Foreign key to the user who created this session
        /// </summary>
        public Guid CreatedById
        {
            get => _createdById;
            set => _createdById = value;
        }

        /// <summary>
        /// Navigation property to the user who created this session
        /// </summary>
        public virtual Modules.User.Models.User CreatedBy
        {
            get => _createdBy;
            set => _createdBy = value;
        }
    }
}
