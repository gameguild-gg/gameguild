using System.ComponentModel.DataAnnotations;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.TestingLab.Models
{
    public class TestingFeedbackForm : BaseEntity
    {
        private Guid _testingRequestId;

        private string _formSchema = string.Empty;

        private bool _isForOnline = true;

        private bool _isForSessions = true;

        [Required]
        public Guid TestingRequestId
        {
            get => _testingRequestId;
            set => _testingRequestId = value;
        }

        [Required]
        public string FormSchema
        {
            get => _formSchema;
            set => _formSchema = value;
        } // JSON

        public bool IsForOnline
        {
            get => _isForOnline;
            set => _isForOnline = value;
        }

        public bool IsForSessions
        {
            get => _isForSessions;
            set => _isForSessions = value;
        }
    }
}
