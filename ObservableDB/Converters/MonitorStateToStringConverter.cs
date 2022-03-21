using ObservableDB.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ObservableDB.Converters
{
    public class MonitorStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            MonitorState s when s == MonitorState.Connected => "Монитор запущен",
            MonitorState s when s == MonitorState.Disconnect => "Монитор отключен",
            MonitorState s when s == MonitorState.Connecting => "Соединение с БД",
            MonitorState s when s == MonitorState.Shutdown => "Отключение от БД",
            _ => "",
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
