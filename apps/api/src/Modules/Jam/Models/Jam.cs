using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Jam.Models
{
    public class Jam : BaseEntity
    {
        private string _name = string.Empty;

        private string _slug = string.Empty;

        private string? _theme;

        private string? _description;

        private string? _rules;

        private string? _submissionCriteria;

        private DateTime _startDate;

        private DateTime _endDate;

        private DateTime? _votingEndDate;

        private int? _maxParticipants;

        private int _participantCount = 0;

        private JamStatus _status = JamStatus.Upcoming;

        private Guid _createdBy;

        [Required, MaxLength(255)]
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        [Required, MaxLength(255)]
        public string Slug
        {
            get => _slug;
            set => _slug = value;
        }

        [MaxLength(500)]
        public string? Theme
        {
            get => _theme;
            set => _theme = value;
        }

        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        public string? Rules
        {
            get => _rules;
            set => _rules = value;
        }

        public string? SubmissionCriteria
        {
            get => _submissionCriteria;
            set => _submissionCriteria = value;
        }

        [Required]
        public DateTime StartDate
        {
            get => _startDate;
            set => _startDate = value;
        }

        [Required]
        public DateTime EndDate
        {
            get => _endDate;
            set => _endDate = value;
        }

        public DateTime? VotingEndDate
        {
            get => _votingEndDate;
            set => _votingEndDate = value;
        }

        public int? MaxParticipants
        {
            get => _maxParticipants;
            set => _maxParticipants = value;
        }

        public int ParticipantCount
        {
            get => _participantCount;
            set => _participantCount = value;
        }

        [Required]
        public JamStatus Status
        {
            get => _status;
            set => _status = value;
        }

        [Required]
        public Guid CreatedBy
        {
            get => _createdBy;
            set => _createdBy = value;
        }
    }
}
