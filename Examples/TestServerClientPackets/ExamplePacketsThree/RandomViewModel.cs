using GalaSoft.MvvmLight.Command;
using Network.Attributes;
using Network.Reactive;
using System.ComponentModel;
using System.Windows.Input;
using TestServerClientPackets.ExamplePacketsOne.Containers;

namespace TestServerClientPackets.ExamplePacketsThree
{
    public class RandomViewModel : ReactiveObject, INotifyPropertyChanged
    {
        private int sliderValue;
        private string text = string.Empty;
        private bool isChecked;
        private Student student;

        public RandomViewModel()
        {
            AddStudent = new RelayCommand(() =>
            {
                Student s = new Student();
                s.Birthday = new Date() { Day = sliderValue };
                s.FirstName = Text;
                s.Lastname = Text;
                Student = s;
            });
        }

        public int SliderValue
        {
            get => sliderValue;
            set
            {
                Sync(ref sliderValue, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SliderValue)));
            }
        }

        [Sync(Network.Enums.SyncDirection.OneWay, 2500)]
        public string Text
        {
            get => text;
            set
            {
                Sync(ref text, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        public bool Checked
        {
            get => isChecked;
            set
            {
                Sync(ref isChecked, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Checked)));
            }
        }

        public Student Student
        {
            get => student;
            set
            {
                Sync(ref student, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Student)));
            }
        }

        [PacketIgnoreProperty]
        public ICommand AddStudent { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
