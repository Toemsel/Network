using Network.Attributes;
using Network.Reactive;
using System.ComponentModel;

namespace TestServerClientPackets.ExamplePacketsThree
{
    public class RandomViewModel : ReactiveObject, INotifyPropertyChanged
    {
        private int sliderValue;
        private string text = string.Empty;
        private bool isChecked;

        public RandomViewModel(int bla) { }

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

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
