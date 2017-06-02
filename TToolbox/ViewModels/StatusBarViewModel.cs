using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using System.Windows;
using System.Windows.Media.Animation;
using TToolbox.DataTypes;

namespace TToolbox.ViewModels
{
    public class StatusBarViewModel : Screen, IHandle<StatusBarMessage>
    {
        private readonly IEventAggregator _eventAggregator;
        private string _statusLabel = "No tip selected.";
        private int _currentProgress = 0;

        public StatusBarViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.Subscribe(this);
        }

        public string StatusLabel
        {
            get { return _statusLabel; }
            set 
            { 
                _statusLabel = value; 
                NotifyOfPropertyChange(() => StatusLabel); 
            }
        }

        public int CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                if (_currentProgress == value)
                {
                    return;
                }

                _currentProgress = value;
                NotifyOfPropertyChange(() => CurrentProgress);
            }
        }

        public void Handle(StatusBarMessage message)
        {
            if (message.LabelText != null)
                StatusLabel = message.LabelText;

            if (message.ProgressPercent != null)
            {
                CurrentProgress = (int)message.ProgressPercent;
            }
        }
    }
}
