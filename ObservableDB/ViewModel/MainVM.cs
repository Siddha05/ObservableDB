using ObservableDB.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ObservableDB.ViewModel
{
    public enum MonitorState
    {
        Disconnect,
        Connecting,
        Connected,
        Shutdown
    }
    public class MainVM : INotifyPropertyChanged
    {
        #region Fields
        private DateTime? _update;
        private string _con_str = @"Data Source=.\SIRSERVER;Initial Catalog=NotifyDB;Integrated Security=True;";
        private RelayCommand? _start;
        private RelayCommand? _stop;
        private RelayCommand? _test_add;
        private RelayCommand? _test_truncate;
        private RelayCommand? _test_edit;
        private ObservableCollection<DBEntity> _data = new ObservableCollection<DBEntity>();
        private string _query = "select [id],[flag],[data] from dbo.tblData;";
        private Dispatcher _dispatcher;
        private bool _start_enable = true;
        private bool _stop_enable;
        private bool _scr_enable = true;
        private MonitorState _monitor_state = MonitorState.Disconnect;
        private Func<Task> _f_update;
        private bool _init;
        #endregion

        #region Properties
        public bool StartEnable
        {
            get => _start_enable;
            set
            {
                _start_enable = value;
                OnPropertyChanged();
            }
        }
        public bool StopEnable
        {
            get => _stop_enable;
            set
            {
                _stop_enable = value;
                OnPropertyChanged();
            }
        }
        public DateTime? UpdateDate
        {
            get { return _update; }
            set { _update = value; OnPropertyChanged(); }
        }
        public string ConnectionString
        {
            get => _con_str;
            set
            {
                _con_str = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<DBEntity> Data => _data;
        public bool ScriptEnable
        {
            get => _scr_enable;
            set
            {
                _scr_enable = value;
                OnPropertyChanged();
            }
        }
        public MonitorState MonitorState
        {
            get => _monitor_state;
            set
            {
                _monitor_state = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Functions
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private async void StopMonitor()
        {
            StopEnable = false;
            MonitorState = MonitorState.Shutdown;
            try
            {
                await SetDependencyAsync(ConnectionString, true);
                StartEnable = true;
                MonitorState = MonitorState.Disconnect;
                _init = false;
            }
            catch (Exception ex)
            {
                LogError(ex);
                StopEnable = true;
                MonitorState = MonitorState.Connected;
            }

        }
        private Task SetDependencyAsync(string connection, bool onlystop = false)
        {
            return Task.Run(() =>
            {
                SqlDependency.Stop(connection);
                if (!onlystop) SqlDependency.Start(connection);
            });
        }
        /// <summary>
        /// В БД должен быть включен Service Broker
        /// </summary>
        private async void StartMonitor()
        {
            StartEnable = false;
            MonitorState = MonitorState.Connecting;
            try
            {
                if (!_init)
                {
                    await FetchData().ContinueWith(_ => _init = true, continuationOptions: TaskContinuationOptions.OnlyOnRanToCompletion);
                    //_init = true;
                }
                await SetDependencyAsync(ConnectionString);
                await RegisterDependencyAsync();
                StopEnable = true;
                MonitorState = MonitorState.Connected;
            }
            catch (Exception ex)
            {
                LogError(ex);
                StartEnable = true;
                MonitorState = MonitorState.Disconnect;
            }
        }
        /// <summary>
        /// Первый раз выполняется в потоке UI, далее в отдельном
        /// </summary>
        private async Task RegisterDependencyAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            var command = connection.CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = _query;
            command.Notification = null;
            try
            {
                var _dependency = new SqlDependency(command);
                _dependency.OnChange += _dependency_OnChange;
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            finally
            {
                connection.Close();
            }
        }
        /// <summary>
        /// Выполняется в отдельном потоке из пула
        /// </summary>
        private void _dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            var d = sender as SqlDependency;
            d.OnChange -= _dependency_OnChange;
            _dispatcher.Invoke(DispatcherPriority.DataBind, _f_update);
            RegisterDependencyAsync();
        }
        /// <summary>
        /// Предполагается, что таблица называется 'dbo.tblData' со столбцами 'id' (not null), 'flag' (not null) и 'data' (allow null)
        /// </summary>
        private async Task FetchData()
        {
            _data.Clear();
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = _query;
                try
                {
                    await connection.OpenAsync();
                    var rd = await cmd.ExecuteReaderAsync();
                    while (rd.Read())
                    {
                        _data.Add(new DBEntity(rd.GetInt32(0), rd.GetBoolean(1), rd.IsDBNull(2) ? null : rd.GetString(2)));
                    }
                    rd?.Close();
                    UpdateDate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
            }
        }
        private async Task ExecuteScript(string path)
        {
            ScriptEnable = false;
            string scr;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            using (StreamReader reader = new StreamReader(stream))
            {
                scr = reader.ReadToEnd();
            }
            var con = new SqlConnection(ConnectionString);
            var cmd = con.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = scr;
            try
            {
                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            finally
            {
                con.Close();
                ScriptEnable = true;
            }
        }
        private void LogError(Exception ex)
        {
            _dispatcher.Invoke(() => MessageBox.Show(ex.Message));
        }
        #endregion

        #region Commands
        public RelayCommand StartCmd => _start ??= new RelayCommand(() => StartMonitor());
        public RelayCommand StopCmd => _stop ??= new RelayCommand(() => StopMonitor());
        public RelayCommand TestAddCmd => _test_add ??= new RelayCommand(async () => await ExecuteScript("ObservableDB.Scripts.AddScript.sql"));
        public RelayCommand TestEditCmd => _test_edit ??= new RelayCommand(async () => await ExecuteScript("ObservableDB.Scripts.EditScript.sql"));
        public RelayCommand TestTruncateCmd => _test_truncate ??= new RelayCommand(async () => await ExecuteScript("ObservableDB.Scripts.TruncateScript.sql"));
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainVM(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _f_update = new Func<Task>(FetchData);
        }

    }
}
