#region Imports

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

#endregion


namespace Libraries.EzeDbCommon
{
	#region Interfaces

	public interface IStatistics
	{
		string Name { get; }
		int Instance { get; }
		string CommitString { get; }

		void PreCommit();
	}

	public interface ICommitHandler
	{
		void Commit(IStatistics statistics);
	}

	public interface IStatisticsFunctionWrapper : IDisposable
	{
	}

	public interface IStatisticsFunctionWrapperFactory
	{
		IStatisticsFunctionWrapper CreateFunctionWrapper(string description);
	}

	#endregion

	#region class StatisticsProvider

	public class StatisticsProvider : IStatistics
	{
		private readonly IDictionary _counters = new LinkedHashtable();
		private readonly IDictionary _countersPersistent = new LinkedHashtable();
		private readonly string _name;
		private readonly IDictionary _ratios = new LinkedHashtable();
		private readonly IDictionary _timers = new LinkedHashtable();
		private readonly IDictionary _timersPersistent = new LinkedHashtable();

		public StatisticsProvider(string name)
		{
			_name = name;
		}

		public virtual string Name
		{
			get { return _name; }
		}

		public int Instance
		{
			get { return 0; }
		}

		public virtual string CommitString
		{
			get
			{
				lock (this)
				{
					var commitBuilder = new StringBuilder();

					foreach (StatisticsCounter counter in _counters.Values)
					{
						commitBuilder.Append(Name).Append(": ").Append(counter).Append(Environment.NewLine);
						if (counter.ResetOnCommit)
						{
							counter.Reset();
						}
					}
					foreach (StatisticsTimer timer in _timers.Values)
					{
						commitBuilder.Append(Name).Append(": ").Append(timer).Append(Environment.NewLine);
						if (timer.ResetOnCommit)
						{
							timer.Reset();
						}
					}
					foreach (StatisticsRatio ratio in _ratios.Values)
					{
						commitBuilder.Append(Name).Append(": ").Append(ratio).Append(Environment.NewLine);
						if (ratio.ResetOnCommit)
						{
							ratio.Reset();
						}
					}

					return commitBuilder.ToString();
				}
			}
		}

		public virtual void PreCommit()
		{
		}

		public ICollection GetCounters()
		{
			return _counters.Values;
		}

		public ICollection GetTimers()
		{
			return _timers.Values;
		}

		public void AddCounter(string counterName, long initialValue, bool resetOnCommit = true)
		{
			lock (this)
			{
				_counters[counterName] = new StatisticsCounter(counterName, initialValue, resetOnCommit);
				_countersPersistent[counterName] = new StatisticsCounter(counterName, initialValue, resetOnCommit);
			}
		}

		public void RemoveCounter(string counterName)
		{
			lock (this)
			{
				_counters.Remove(counterName);
				_countersPersistent.Remove(counterName);
			}
		}

		public void AddRatio(string name, bool resetOnCommit = true)
		{
			lock (this)
			{
				_ratios[name] = new StatisticsRatio(name, resetOnCommit);
			}
		}

		public void RemoveRatio(string name)
		{
			lock (this)
			{
				_ratios.Remove(name);
			}
		}

		public double CalculateRatioQuotient(string name)
		{
			lock (this)
			{
				var ratio = (StatisticsRatio)_ratios[name];
				if (ratio != null)
				{
					return ratio.CalculateRatio();
				}
			}

			return 0;
		}

		public StatisticsRatio GetRatio(string name)
		{
			lock (this)
			{
				return (StatisticsRatio)_ratios[name];
			}
		}

		public void AddToRatio(string name, long antecedentModifier, long consequentModifier)
		{
			lock (this)
			{
				var ratio = (StatisticsRatio)_ratios[name];
				if (ratio != null)
				{
					ratio.Add(antecedentModifier, consequentModifier);
				}
			}
		}

		public void IncCounter(string counterName, long incValue)
		{
			lock (this)
			{
				var counter = (StatisticsCounter)_counters[counterName];
				if (counter != null)
				{
					counter.Value = counter.Value + incValue;
				}

				counter = (StatisticsCounter)_countersPersistent[counterName];
				if (counter != null)
				{
					counter.Value = counter.Value + incValue;
				}
			}
		}

		public void DecCounter(string counterName, long decValue)
		{
			lock (this)
			{
				var counter = (StatisticsCounter)_counters[counterName];
				if (counter != null)
				{
					counter.Value = counter.Value - decValue;
				}

				counter = (StatisticsCounter)_countersPersistent[counterName];
				if (counter != null)
				{
					counter.Value = counter.Value - decValue;
				}
			}
		}

		public void SetCounter(string counterName, long counterValue)
		{
			lock (this)
			{
				var counter = (StatisticsCounter)_counters[counterName];
				if (counter != null)
				{
					counter.Value = counterValue;
				}

				counter = (StatisticsCounter)_countersPersistent[counterName];
				if (counter != null)
				{
					counter.Value = counterValue;
				}
			}
		}

		public long GetCounter(string counterName)
		{
			lock (this)
			{
				var counter = (StatisticsCounter)_counters[counterName];
				if (counter != null)
				{
					return counter.Value;
				}

				return 0;
			}
		}

		public double GetGaugeCounter(string counterName)
		{
			lock (this)
			{
				var counter = (StatisticsCounter)_countersPersistent[counterName];
				if (counter != null)
				{
					long value = counter.Value;
					TimeSpan elapsed = counter.Elapsed;
					counter.Value = 0;

					/*
					 * We need to convert the absolute (in SNMP speak, "counter") variable
					 * to a "gauge" type variable so we don't have to worry about 
					 * process restarts causing counters to rollover in weird ways.
					 */

					if (value == 0)
					{
						return 0;
					}

					return value / elapsed.TotalHours;
				}

				return 0;
			}
		}

		public void AddTimer(string timerName, bool resetOnCommit = true, bool recordMinMax = false)
		{
			lock (this)
			{
				_timers[timerName] = new StatisticsTimer(timerName, resetOnCommit) { RecordMinMax = recordMinMax };
				_timersPersistent[timerName] = new StatisticsTimer(timerName, resetOnCommit);
			}
		}

		public void RemoveTimer(string timerName)
		{
			lock (this)
			{
				_timers.Remove(timerName);
				_timersPersistent.Remove(timerName);
			}
		}

		public void StartTimer(string timerName)
		{
			lock (this)
			{
				var timer = (StatisticsTimer)_timers[timerName];
				if (timer != null)
				{
					timer.Start();
				}

				timer = (StatisticsTimer)_timersPersistent[timerName];
				if (timer != null)
				{
					timer.Start();
				}
			}
		}

		public void ResetTimer(string timerName)
		{
			lock (this)
			{
				var timer = (StatisticsTimer)_timers[timerName];
				if (timer != null)
				{
					timer.Reset();
				}
			}
		}

		public void StopTimer(string timerName)
		{
			lock (this)
			{
				var timer = (StatisticsTimer)_timers[timerName];
				if (timer != null)
				{
					timer.Stop();
				}


				timer = (StatisticsTimer)_timersPersistent[timerName];
				if (timer != null)
				{
					timer.Stop();
				}
			}
		}

		public long GetTotalTime(string timerName)
		{
			lock (this)
			{
				var timer = (StatisticsTimer)_timers[timerName];
				if (timer != null)
				{
					return timer.TotalTime;
				}

				return 0;
			}
		}

		public long GetTotalTimePersistent(string timerName)
		{
			lock (this)
			{
				var timer = (StatisticsTimer)_timersPersistent[timerName];
				if (timer != null)
				{
					return timer.TotalTime;
				}

				return 0;
			}
		}
	}

	#endregion

	#region class StatisticsManager

	public class StatisticsManager
	{
		public delegate void CommitHandler(IStatistics statistics);

		private readonly Timer _clock;
		private readonly TimeSpan _interval;
		private readonly Hashtable _statistics = new Hashtable();

		public StatisticsManager(TimeSpan interval)
		{
			_interval = interval;
			_clock = new Timer(timer_Tick, this, interval, interval);
		}

		public TimeSpan Interval
		{
			get { return _interval; }
			set
			{
				lock (this)
				{
					_clock.Change(value, value);
				}
			}
		}

		public event CommitHandler Commit;

		public void Shutdown()
		{
			_clock.Dispose();
			Commit = null;
		}

		public void StartManager()
		{
			lock (this)
			{
				_clock.Change(_interval, _interval);
			}
		}

		public void StopManager()
		{
			lock (this)
			{
				_clock.Change(Timeout.Infinite, Timeout.Infinite);
			}
		}

		public IStatistics GetStatistic(string name)
		{
			lock (this)
			{
				return (IStatistics)_statistics[new StatisticInstance(name)];
			}
		}

		public IStatistics GetStatistics(string name, int instance)
		{
			lock (this)
			{
				return (IStatistics)_statistics[new StatisticInstance(name, instance)];
			}
		}

		public void AddStatistic(IStatistics statistic)
		{
			lock (this)
			{
				_statistics[new StatisticInstance(statistic)] = statistic;
			}
		}

		public void RemoveStatistic(IStatistics statistic)
		{
			lock (this)
			{
				var statisticInstance = new StatisticInstance(statistic);
				if (_statistics.ContainsKey(statisticInstance))
				{
					_statistics.Remove(statisticInstance);
				}
			}
		}

		public void timer_Tick(object sender)
		{
			try
			{
				if (sender == this)
				{
					// We don't want to lock here because we could easily deadlock with 
					// a thread that was holding a lock in a particular subsystem and trying
					// to update a counter.

					IStatistics[] activeStatistics;
					lock (this)
					{
						activeStatistics = new IStatistics[_statistics.Count];
						_statistics.Values.CopyTo(activeStatistics, 0);
					}

					foreach (IStatistics statistics in activeStatistics)
					{
						statistics.PreCommit();
						Commit(statistics);
					}
				}
			}
			catch (Exception ex)
			{
				//Runtime.Log.Debug("Statistics Manager failed to report ", ex);
			}
		}

		private struct StatisticInstance
		{
			public readonly int Instance;
			public readonly string Name;

			public StatisticInstance(IStatistics statistics)
			{
				Instance = statistics.Instance;
				Name = statistics.Name;
			}

			public StatisticInstance(string name, int instance)
			{
				Instance = instance;
				Name = name;
			}

			public StatisticInstance(string name)
				: this()
			{
				Name = name;
				Instance = 0;
			}

			public bool Equals(StatisticInstance other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return other.Instance == Instance && Equals(other.Name, Name);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof(StatisticInstance)) return false;
				return Equals((StatisticInstance)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Instance * 397) ^ Name.GetHashCode();
				}
			}
		}
	}

	#endregion

	#region class StatisticsTimer

	public class StatisticsTimer
	{
		public readonly bool ResetOnCommit;
		private readonly string _name;

		private readonly ListDictionary _timersByThread = new ListDictionary();
		private long _totalTime;

		public StatisticsTimer(string name, bool resetOnCommit)
		{
			_name = name;
			ResetOnCommit = resetOnCommit;
			Reset();
		}

		public string Name
		{
			get { return _name; }
		}

		public bool RecordMinMax { get; set; }

		public long TotalTime
		{
			get { return Interlocked.Read(ref _totalTime); }
		}

		public TimeSpan MinTime { get; private set; }

		public TimeSpan MaxTime { get; private set; }

		public bool IsStopped
		{
			get
			{
				lock (this)
				{
					return _timersByThread.Contains(Thread.CurrentThread);
				}
			}
		}

		public void Start()
		{
			Thread thread = Thread.CurrentThread;
			lock (this)
			{
				_timersByThread[thread] = Stopwatch.StartNew();
			}
		}

		public void Stop()
		{
			Thread thread = Thread.CurrentThread;
			lock (this)
			{
				var timer = (Stopwatch)_timersByThread[thread];
				timer.Stop();
				_timersByThread.Remove(thread);
				if (RecordMinMax)
				{
					if (timer.Elapsed < MinTime)
					{
						MinTime = timer.Elapsed;
					}
					if (timer.Elapsed > MaxTime)
					{
						MaxTime = timer.Elapsed;
					}
				}
				Interlocked.Add(ref _totalTime, timer.Elapsed.Ticks);
			}
		}

		public void Reset()
		{
			// this does not change the start/stop status of the timer
			// just want to set the value to 0
			Interlocked.Exchange(ref _totalTime, 0);
			MinTime = TimeSpan.MaxValue;
			MaxTime = TimeSpan.MinValue;
		}

		public override string ToString()
		{
			string toString = _name + " = " + TimeSpan.FromTicks(TotalTime);
			if (MinTime != TimeSpan.MaxValue)
			{
				toString = string.Format("{0} (min: {1}, max: {2})", toString, MinTime, MaxTime);
			}
			return toString;
		}
	}

	#endregion

	#region class StatisticsCompression

	public class StatisticsRatio
	{
		public readonly bool ResetOnCommit;
		private readonly string _name;

		private long _antecedentTerm;
		private long _consequentTerm;

		public StatisticsRatio(string name, bool resetOnCommit)
		{
			_name = name;
			ResetOnCommit = resetOnCommit;
		}

		public string Name
		{
			get { return _name; }
		}

		public double AntecedentTerm
		{
			get { return _antecedentTerm; }
		}

		public double ConsequentTerm
		{
			get { return _consequentTerm; }
		}

		public void Add(long antecedentModifier, long consequentModifier)
		{
			_antecedentTerm += antecedentModifier;
			_consequentTerm += consequentModifier;
		}

		public double CalculateRatio()
		{
			if (_consequentTerm > 0)
			{
				return _antecedentTerm / (double)_consequentTerm;
			}

			return 0;
		}

		public void Reset()
		{
			_antecedentTerm = _consequentTerm = 0;
		}

		public override string ToString()
		{
			return _name + " = " + CalculateRatio().ToString("N2");
		}
	}

	#endregion

	#region class StatisticsCounter

	public class StatisticsCounter
	{
		private readonly long _initialValue;
		private readonly string _name;
		public bool ResetOnCommit;
		private DateTime _periodStart;
		private long _value;

		public StatisticsCounter(string name, long initialValue, bool resetOnCommit = true)
		{
			_name = name;
			_value = _initialValue = initialValue;
			_periodStart = DateTime.Now;
			ResetOnCommit = resetOnCommit;
		}

		public string Name
		{
			get { return _name; }
		}

		public long Value
		{
			get { return _value; }
			set { _value = value; }
		}

		public long InitialValue
		{
			get { return _initialValue; }
		}

		/// <summary>
		///     Get the time that has elapsed since the counter was created AND
		///     also reset the period "start" variable (for the next period).
		/// </summary>
		public TimeSpan Elapsed
		{
			get
			{
				DateTime now = DateTime.Now;
				TimeSpan timeSpan = now - _periodStart;

				_periodStart = now;

				return timeSpan;
			}
		}

		public void Reset()
		{
			_value = _initialValue;
		}

		public override string ToString()
		{
			return _name + " = " + _value;
		}
	}

	#endregion

	#region class StatisticsCountAndTime

	public class StatisticsCountAndTime
	{
		public const string CounterSuffix = " Counter";
		public const string TimerSuffix = " Timer";

		private readonly StatisticsProvider _provider;

		public StatisticsCountAndTime(StatisticsProvider provider)
		{
			_provider = provider;
		}

		public void Add(params string[] name)
		{
			foreach (string s in name)
			{
				_provider.AddCounter(s + CounterSuffix, 0);
				_provider.AddTimer(s + TimerSuffix);
			}
		}

		public void Remove(params string[] name)
		{
			foreach (string s in name)
			{
				_provider.RemoveCounter(s + CounterSuffix);
				_provider.RemoveTimer(s + TimerSuffix);
			}
		}

		public void RecordMinMax(params string[] name)
		{
			foreach (StatisticsTimer timer in _provider.GetTimers())
			{
				if (name.Any(s => timer.Name == s + TimerSuffix))
				{
					timer.RecordMinMax = true;
				}
			}
		}

		public void Start(string name)
		{
			_provider.IncCounter(name + CounterSuffix, 1);
			_provider.StartTimer(name + TimerSuffix);
		}

		public void Stop(string name)
		{
			_provider.StopTimer(name + TimerSuffix);
		}

		public StatRunner Run(string name)
		{
			return new StatRunner(this, name);
		}

		public struct StatRunner : IDisposable
		{
			private readonly string _name;
			private readonly StatisticsCountAndTime _parent;

			internal StatRunner(StatisticsCountAndTime parent, string name)
			{
				_parent = parent;
				_name = name;
				_parent.Start(_name);
			}

			public void Dispose()
			{
				_parent.Stop(_name);
			}
		}
	}

	#endregion

	#region class StatisticsLogger

	public class StatisticsLogger
	{
		public void RegisterLogger(StatisticsManager statisticsManager)
		{
			statisticsManager.Commit += statistics_Commit;
		}

		private void statistics_Commit(IStatistics statistics)
		{
			string commitString = statistics.CommitString;
			if (!string.IsNullOrEmpty(commitString))
			{
				//Runtime.Log.Statistics(statistics.Name, statistics.Instance, commitString);
			}
		}
	}

	#endregion
}