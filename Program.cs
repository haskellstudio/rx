using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;

namespace rx
{
    public class Observer<T> : IObserver<T>
    {
        private Action<T> _onNext =
            p =>
            {
                Console.WriteLine("Observer: default onNext");
            };

        private Action _onComplete =
            () =>
            {
                Console.WriteLine("Observer: default onComplete");
            };

        private Action<Exception> _onError =
            ex =>
            {
                Console.WriteLine("Observer: default onError");
            };


        public Observer(Action<T> nxt, Action<Exception> err, Action complete)
        {
            _onNext = nxt;
            _onError = err;
            _onComplete = complete;
        }

        public Observer(Action<T> nxt, Action<Exception> err)
        {
            _onNext = nxt;
            _onError = err;
        }

        public Observer(Action<T> nxt)
        {
            _onNext = nxt;
        }

        public void OnCompleted()
        {
            _onComplete();
        }

        public void OnError(Exception error)
        {
            _onError(error);
        }

        public void OnNext(T value)
        {
            _onNext(value);
        }
    }

    public class DisposeObject<T> : IDisposable
    {
        private List<IObserver<T>> _observers;
        private BaseSubject<T> _subject;
        private IObserver<T> _observer;

        public DisposeObject(List<IObserver<T>> observers, BaseSubject<T> subject, IObserver<T> observer)
        {
            _observers = observers;
            _subject = subject;
            _observer = observer;
        }
        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
            if (_observers.Count == 0)
                _subject._running = false;
        }
    }



    public interface IScheduler
    {
        void Schedule(Action action);

        // void ScheduleInThread(Action action);
        //  void ScheduleInThread(Action action, CancellationToken cancellationToken);

        void Schedule<T>(Action<T> action, T value);


        void Schedule<T>(Action action, CancellationToken cancellationToken);
        void Schedule<T>(Action<T> action, T value, CancellationToken cancellationToken);

        //  void ScheduleInThread<T>(Action<T> action, T value);

        //  void ScheduleInThread<T>(Action<T> action, T value, CancellationToken cancellationToken);
    }


    public class TaskScheduler : IScheduler
    {

        private CancellationToken _cancellationToken;
        public TaskScheduler()
        {

        }


        public TaskScheduler(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public void Schedule(Action action)
        {
            Task.Factory.StartNew(action);
        }

        public void Schedule<T>(Action action, CancellationToken cancellationToken)
        {

            Task.Factory.StartNew(action, cancellationToken);
        }

        public void Schedule<T>(Action<T> action, T value)
        {
            Action fun = () => { action(value); };
            Task.Factory.StartNew(fun);
        }

        public void Schedule<T>(Action<T> action, T value, CancellationToken cancellationToken)
        {

            Action<object> actionObject = (object o) => { action(value); };
            Task.Factory.StartNew(actionObject, cancellationToken);
        }

        //public void ScheduleInThread(Action action)
        //{
        //    Task.Factory.StartNew(action);
        //}

        //public void ScheduleInThread(Action action, CancellationToken cancellationToken)
        //{
        //    Task.Factory.StartNew(action, cancellationToken);
        //}

        //public void ScheduleInThread<T>(Action<T> action , T value)
        //{
        //    Action fun = () => { action(value); };
        //    Task.Factory.StartNew(fun);

        //    //Action<object> actionObject = (object o) => { action(value); };
        //    //Task.Factory.StartNew(actionObject, (object)value);
        //}

        //public void ScheduleInThread<T>(Action<T> action, T value, CancellationToken cancellationToken)
        //{
        //    Action fun = () => action(value);
        //    Task.Factory.StartNew(fun, cancellationToken);
        //}
    }





    public abstract class BaseSubject<T> : IObservable<T>
    {
        private IScheduler _scheduler = new CurrentThreadScheduler();//TaskScheduler();

        public List<IObserver<T>> _observers;

        public bool _running;

        public IScheduler _Scheduler
        {
            get
            {
                return _scheduler;
            }

            set
            {
                _scheduler = value;
            }
        }

        public abstract void execute();

        public BaseSubject()
        {
            _observers = new List<IObserver<T>>();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {

            IDisposable disposable = new DisposeObject<T>(_observers, this, observer);
            _observers.Add(observer);

            if (!_running)
            {
                _running = true;
                execute();
            }

            return disposable;
        }

        public IDisposable ColdSubscribe(IObserver<T> observer)
        {
            IDisposable disposable = new DisposeObject<T>(_observers, this, observer);

            _observers.Add(observer);

            return disposable;
        }


        protected virtual void notifyValue(T value)
        {
            foreach (var i in _observers)
                i.OnNext(value);
        }

        protected virtual void notifyError(Exception e)
        {
            foreach (var i in _observers)
                i.OnError(e);
        }

        protected virtual void notifyComplete()
        {
            foreach (var i in _observers)
                i.OnCompleted();
        }




    }




    public class Subject<T> : BaseSubject<T>, IObserver<T>, IObservable<T>, IDisposable
    {
        //private Action<T> _onNxt =
        //    p =>
        //    {
        //        Console.WriteLine("Subject: default _onNxt");
        //    };

        //private Action _onComplete =
        //    () =>
        //    {
        //        Console.WriteLine("Subject: default _onComplete");
        //    };

        //private Action<Exception> _onErr =
        //    e =>
        //    {
        //        Console.WriteLine("Subject: default _onErr");
        //    };


        //public Subject(Action<T> onNxt, Action<Exception> err, Action onComplete)
        //{
        //    _onNxt = onNxt;
        //}

        public void Dispose()
        {
            notifyComplete();
        }

        public override void execute()
        {
        }

        public void OnCompleted()
        {
            notifyComplete();
        }

        public void OnError(Exception error)
        {
            notifyError(error);
        }

        public void OnNext(T value)
        {
            notifyValue(value);
        }
    }

    public class ReturnSubject<T> : BaseSubject<T>
    {
        private T _value;
        public override void execute()
        {
            notifyValue(_value);
            notifyComplete();
        }

        public ReturnSubject(T value)
        {
            this._value = value;
        }
    }

    public class ReturnSubjectArray<T> : BaseSubject<T>
    {
        private T[] _array;

        public ReturnSubjectArray(T[] array)
        {
            _array = array;
        }

        public override void execute()
        {
            foreach (var i in _array)
            {
                notifyValue(i);
            };

            notifyComplete();
        }
    }


    public class ReturnSubjectCollection<T> : BaseSubject<T>
    {
        private IEnumerable<T> _collection;


        public ReturnSubjectCollection(IEnumerable<T> collection)
        {
            _collection = collection;
        }

        public override void execute()
        {
            foreach (var i in _collection)
            {
                notifyValue(i);
            }

            notifyComplete();
        }
    }


    public class RangeSubject : BaseSubject<int>  // RangeSubject(10, 3) =>  10, 11, 12
    {
        private int _value;
        private int _xtime;

        public RangeSubject(int value, int xtime)
        {
            _value = value;
            _xtime = xtime;
        }

        public RangeSubject(int value, int xtime, IScheduler scheduler)
        {
            _value = value;
            _xtime = xtime;
            _Scheduler = scheduler;
        }


        public class ThreadExecuter
        {
            public int __value;
            public int __xtime;

            public List<IObserver<int>> __observers;

            public IScheduler __scheduler;

            //private int repeatCounter = 0;

            public void Execute()
            {

                for (var i = 0; i < __xtime; i++)
                {
                    foreach (var j in __observers)
                    {
                        j.OnNext(__value + i);
                    }
                }

                foreach (var j in __observers)
                {
                    j.OnCompleted();
                }


                //Action<int> action = null;

                //action =
                //    (int value) =>
                //    {
                //        if (__observers.Count <= 0)
                //            return;

                //        foreach (var i in __observers)
                //            i.OnNext(value);

                //        if(repeatCounter < __xtime - 1)
                //        {
                //            repeatCounter++;
                //            __scheduler.Schedule(action, ++value);
                //        }
                //        else
                //        {
                //            foreach (var i in __observers)
                //                i.OnCompleted();
                //        }

                //        return;
                //    };

                //action(__value);
            }
        }
        public override void execute()
        {
            ThreadExecuter te = new ThreadExecuter
            {
                __value = _value,
                __observers = _observers,
                __scheduler = _Scheduler,
                __xtime = _xtime
            };

            _Scheduler.Schedule(te.Execute);

            //for (int i = 0; i < _xtime; i++)
            //{
            //    notifyValue(_value + i);
            //}
            //notifyComplete();
        }
    }





    public class EmptySubject<T> : BaseSubject<T>
    {
        public EmptySubject() { }

        public override void execute()
        {
            foreach (var i in _observers)
            {
                i.OnNext(default(T));
                i.OnCompleted();
            }
        }
    }


    public class RepeatSubject<T> : BaseSubject<T>
    {
        private T _value;
        private int _repeatCount;
        private bool _repeatendless = true;

        public class ThreadExecuter<TT>
        {
            public TT __value;
            public int __repeatcount;
            public bool __repeatendless;

            //public IScheduler __scheduler;
            public List<IObserver<TT>> __observers;



            public void exec()
            {
                if (__observers.Count <= 0)
                    return;
                if (!__repeatendless)
                {
                    for (var i = __repeatcount; i > 0; i--)
                    {
                        foreach (var j in __observers)
                        {
                            j.OnNext(__value);
                        }
                    }

                    foreach (var i in __observers)
                    {
                        i.OnCompleted();
                    }

                }
                else
                {
                    while (true)
                    {
                        foreach (var i in __observers)
                        {
                            i.OnNext(__value);
                        }
                    }
                }
            }
        }
        public RepeatSubject(T value, IScheduler i = null)
        {
            _value = value;
            if (i != null)
            {
                _Scheduler = i;
            }
        }

        public RepeatSubject(T value, int repeatCount, IScheduler i = null)
        {
            _value = value;
            _repeatCount = repeatCount;
            _repeatendless = false;
            if (i != null)
            {
                _Scheduler = i;
            }
        }


        public override void execute()
        {
            ThreadExecuter<T> te = new ThreadExecuter<T>()
            {
                __value = _value,
                __repeatcount = _repeatCount,
                __repeatendless = _repeatendless,
                __observers = _observers,
                //__scheduler = _Scheduler
            };
            _Scheduler.Schedule(te.exec);
            //_Scheduler.ScheduleInThread(te.exec);
        }
    }


    public class ThrowSubject<T> : BaseSubject<T>
    {
        private Exception _execption;

        public ThrowSubject(Exception e)
        {
            _execption = e;
        }

        public override void execute()
        {
            notifyError(_execption);
        }
    }


    public class NeverSubject<T> : BaseSubject<T>
    {
        public override void execute()
        {

        }
    }


    public class CurrentThreadScheduler : IScheduler
    {
        public void Schedule(Action action)
        {
            action();
        }

        public void Schedule<T>(Action action, CancellationToken cancellationToken)
        {
            action();
        }

        public void Schedule<T>(Action<T> action, T value)
        {
            action(value);
        }

        public void Schedule<T>(Action<T> action, T value, CancellationToken cancellationToken)
        {
            action(value);
        }
    }





    public class ObserveOnSubject<T> : BaseSubject<T>
    {
        //  private IObservable<T> _source;
        private BaseSubject<T> _subject;
        private IDisposable _subscriped;


        public ObserveOnSubject(IObservable<T> source)
        {
            //  _source = source;
            _Scheduler = new CurrentThreadScheduler();
            _subject = (BaseSubject<T>)source;

            var observer = new Observer<T>(
                value =>
                {
                    notifyValue(value);
                }
                ,
                e =>
                {
                    notifyError(e);
                }
                ,
                () =>
                {
                    notifyComplete();
                }
                );

            _subscriped = _subject.ColdSubscribe(observer);

        }

        public ObserveOnSubject(IObservable<T> source, IScheduler scheduler) : this(source)
        {
            _subject._Scheduler = scheduler;
            base._Scheduler = scheduler;
        }
        public override void execute()
        {
            _Scheduler.Schedule(
                () =>
                {
                    try
                    {
                        _subject.execute();
                    }
                    catch (Exception e)
                    {
                        notifyError(e);
                    }
                }

                );
        }
    }



    //public class MinSubjectInt : BaseSubject<int>
    //{
    //    //private IObservable<int> _source;
    //    private IDisposable _subcriped;
    //    private BaseSubject<int> _subject;

    //    private int _minValue;
    //    private bool _valueSet = false;

    //    public MinSubjectInt(IObservable<int> source)
    //    {
    //        //_source = source;
    //        _subject = (BaseSubject<int>) source;
    //        var observer = new Observer<int>(
    //            v =>
    //            {
    //                innerExecute(v);
    //            },
    //            e =>
    //            {
    //                notifyError(e);
    //            },
    //            () =>
    //            {
    //                notifyComplete();
    //            }


    //            );

    //        _subcriped = _subject.ColdSubscribe(observer);
    //    }

    //    private void innerExecute(int value)
    //    {
    //        if(!_valueSet)
    //        {
    //            _minValue = value;
    //            _valueSet = true;
    //        }
    //        else
    //        {
    //            if(value < _minValue)
    //            {
    //                _minValue = value;
    //            }
    //        }
    //    }
    //    public override void execute()
    //    {
    //        try
    //        {
    //            _subject.execute();
    //            notifyValue(_minValue);

    //        }
    //        catch(Exception e)
    //        {
    //            notifyError(e);
    //        }
    //    }
    //}



    public class MinSubject<T> : BaseSubject<T> where T : IComparable<T>
    {
        //private IObservable<int> _source;
        private IDisposable _subcriped;
        private BaseSubject<T> _subject;

        private T _minValue;
        private bool _valueSet = false;

        public MinSubject(IObservable<T> source)
        {
            //_source = source;
            _subject = (BaseSubject<T>)source;
            var observer = new Observer<T>(
                v =>
                {
                    innerExecute(v);
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }


                );

            _subcriped = _subject.ColdSubscribe(observer);
        }

        private void innerExecute(T value)
        {
            if (!_valueSet)
            {
                _minValue = value;
                _valueSet = true;
            }
            else
            {
                if (value.CompareTo(_minValue) < 0)
                {
                    _minValue = value;
                }
            }
        }
        public override void execute()
        {
            try
            {
                _subject.execute();
                notifyValue(_minValue);

            }
            catch (Exception e)
            {
                notifyError(e);
            }
        }
    }



    public class DistinctSubject<T> : BaseSubject<T>
    {
        private IDisposable _subscriped;
        private BaseSubject<T> _subject;
        private List<T> _values = new List<T>();

        public DistinctSubject(IObservable<T> source)
        {
            _subject = (BaseSubject<T>)source;


            var observer = new Observer<T>(
                v =>
                {
                    if (!_values.Contains(v))
                    {
                        notifyValue(v);
                        _values.Add(v);
                    }
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }
            );

            _subscriped = _subject.ColdSubscribe(observer);
        }

        public override void execute()
        {
            try
            {
                _subject.execute();
            }
            catch (Exception e)
            {
                notifyError(e);
            }
        }
    }




    public class ElementAtSubject<T> : BaseSubject<T>
    {
        //  private IObservable<T> _source;
        private int _index;
        private int _i = 0;
        private IDisposable _subscriped;
        private BaseSubject<T> _subject;

        public ElementAtSubject(IObservable<T> source, int index)
        {
            _subject = (BaseSubject<T>)source;
            _index = index;
            var observer = new Observer<T>(
                v =>
                {
                    if (_i == _index)
                    {
                        notifyValue(v);
                        _i++;
                    }
                    else
                    {
                        _i++;
                    }
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }

            );

            _subscriped = _subject.ColdSubscribe(observer);
        }

        public override void execute()
        {

            try
            {
                _subject.execute();
            }
            catch (Exception e)
            {
                notifyError(e);
            }
        }



    }

    public class GenerateSubject<T> : BaseSubject<T>
    {
        private T _initValue;
        private Predicate<T> _condition;
        private Func<T, T> _iterate;
        private Func<T, T> _resultSelector;

        public GenerateSubject(T initValue, Predicate<T> condition,
                              Func<T, T> iterate, Func<T, T> resultSelector,
                               IScheduler scheduler)
        {
            _initValue = initValue;
            _condition = condition;
            _iterate = iterate;
            _resultSelector = resultSelector;

            if (scheduler == null)
            {
                this._Scheduler = new CurrentThreadScheduler(); //TaskScheduler();//
            }
            else
            {
                _Scheduler = scheduler;
            }

        }

        public class ThreadExecuter<TT>
        {
            public TT __intValue;
            public Predicate<TT> __condition;
            public Func<TT, TT> __iterate;
            public Func<TT, TT> __reslutselector;
            public List<IObserver<TT>> __observers;
            public IScheduler __scheduler;



            public void Execute()
            {
                Action<TT> action = null;

                action = (TT value) =>
                {
                    if (__observers.Count < 0)
                        return;
                    TT lv = value;
                    while (__condition(lv))
                    {
                        var selectorValue = __reslutselector(lv);
                        foreach (var i in __observers)
                            i.OnNext(selectorValue);
                        lv = __iterate(lv);
                    }

                    foreach(var i in __observers)
                    {
                        i.OnCompleted();
                    }

                };

                action(__intValue);
            }

        }

        public override void execute()
        {
            ThreadExecuter<T> threadExecuter = new ThreadExecuter<T>()
            {
                __intValue = _initValue,
                __observers = _observers,
                __condition = _condition,
                __iterate = _iterate,
                __reslutselector = _resultSelector,
                __scheduler = _Scheduler
            };

            _Scheduler.Schedule(threadExecuter.Execute);
        }

    }



    public class BufferSubject<T, T1> : BaseSubject<T>
    // T  the new  Observable type
    // T1 original Observable type

    {
        private int _bufferCount = 0;
        private int _actualCount = 0;
        private IDisposable _subscriped;
        private BaseSubject<T1> _subject;
        private IList<T1> _buffer;

        public BufferSubject(int count)
        {
            Type type = typeof(T);
            Type itemType = type.GetGenericArguments()[0];
            Type listType = typeof(List<>);
            Type constructed = listType.MakeGenericType(itemType);
            _buffer = (List<T1>)Activator.CreateInstance(constructed);
            _bufferCount = count;
        }

        public void Start(IObservable<T1> source)
        {
            _subject = (BaseSubject<T1>)source;

            var observer = new Observer<T1>(
                
                v =>
                {
                    if(_actualCount < _bufferCount)
                    {
                        MethodInfo methodInfo = _buffer.GetType().GetMethod("Add");
                        methodInfo.Invoke(_buffer, new object[] { v });
                        _actualCount++;
                    }
                }
                ,
                e =>
                {
                    notifyError(e);
                },

                () => { }
                
                );


            _subscriped = _subject.ColdSubscribe(observer);
        }

        public override void execute()
        {
            try
            {
                _subject.execute();
                notifyValue((T)_buffer);
                notifyComplete();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }



    }







    public class DelaySubject<T> : BaseSubject<T>
    {
        private IDisposable _subscriped;
        private BaseSubject<T> _subject;
        private TimeSpan _dueTime;

        public DelaySubject(IObservable<T> source, TimeSpan duetime)
        {
            _subject = (BaseSubject<T>)source;
            _dueTime = duetime;

            var observer = new Observer<T>(
                v =>
                {
                    notifyValue(v);
                },
                e =>
                {
                    notifyError(e);
                }
                ,
                () => notifyComplete()
                );
            _subscriped = _subject.ColdSubscribe(observer);

        }


        public override void execute()
        {
            Timer timer = new Timer(
                _ =>
                {
                    try
                    {
                        _subject.execute();
                    }
                    catch (Exception e)
                    {
                        notifyError(e);
                    }
                },
                null,
                _dueTime,
                new TimeSpan(-1)

                );
        }


    }


    public class CatchSubject<T> : BaseSubject<T>
    {
        private BaseSubject<T> _fstsubject;
        private BaseSubject<T> _sndsubject;
        private IDisposable _fstsubscriped;
        private IDisposable _sndsubscriped;

        public CatchSubject(IObservable<T> fst, IObservable<T> snd)
        {
            _fstsubject = (BaseSubject<T>)fst;
            _sndsubject = (BaseSubject<T>)snd;

            var observer = new Observer<T>(
                v =>
                {
                    notifyValue(v);
                },

                e =>
                {
                    ErrorExecute(e);
                },
                () =>
                {
                    notifyComplete();
                }
                );

            _fstsubscriped = _fstsubject.ColdSubscribe(observer);
        }


        private void ErrorExecute(Exception exception)
        {
            Console.WriteLine("error execute");
            var observer = new Observer<T>(
                v =>
                {
                    notifyValue(v);
                },

                e =>
                {
                    notifyError(exception);
                },
                () =>
                {
                    notifyComplete();
                }
                
                );
            _sndsubscriped = _sndsubject.Subscribe(observer);

        }


        public override void execute()
        {
            try
            {
                _fstsubject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }

    }


    public class FinallySubject<T> : BaseSubject<T>
    {
        private BaseSubject<T> _subject;
        private IDisposable _subscriped;
        private Action _action;


        public FinallySubject(IObservable<T> source, Action action)
        {
            _subject = (BaseSubject<T>)source;
            _action = action;

            var observer = new Observer<T>(
                v =>
                {
                    notifyValue(v);
                },
                e =>
                {
                    notifyError(e);
                    _action();
                },
                () =>
                {
                    notifyComplete();
                    _action();
                }
                );
            _subscriped = _subject.ColdSubscribe(observer);


        }

        public override void execute()
        {
            try
            {
                _subject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
                _action();
            }
        }
    }



    public class AmbSubject<T> : BaseSubject<T>
    {
        private IDisposable _fstsubscriped;
        private IDisposable _sndsubscriped;


        private BaseSubject<T> _fst;
        private BaseSubject<T> _snd;

        private TimeSpan dueTime;
        private bool _fstReacted;
        private bool _sndReacted;


        public AmbSubject(IObservable<T> fst, IObservable<T> snd)
        {
            _fst = (BaseSubject<T>)fst;
            _snd = (BaseSubject<T>)snd;

            var fstobserver = new Observer<T>(
                v =>
                {
                    try
                    {
                        if (!_sndReacted)
                        {
                            _fstReacted = true;
                            notifyValue(v);
                        }
                    }
                    catch(Exception e)
                    {
                        notifyError(e);
                    }

                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }
                
                
                );

            var sndobserver = new Observer<T>(
                v =>
                {
                    try
                    {
                        if (!_fstReacted)
                        {
                            _sndReacted = true;
                            notifyValue(v);
                        }
                    }
                    catch (Exception e)
                    {
                        notifyError(e);
                    }
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }

                );

            _fstsubscriped = _fst.ColdSubscribe(fstobserver);

            _sndsubscriped = _snd.ColdSubscribe(sndobserver);
        }


        public override void execute()
        {
            _fst.execute();
            _snd.execute();
        }



    }


    public class AnySubject<T> : BaseSubject<bool>
    {
     
        private IDisposable _subscriped;
        private BaseSubject<T> _subject;
        private bool _passed;

        public AnySubject(IObservable<T> source)
        {
            _subject = (BaseSubject<T>)source;

            var observer = new Observer<T>(
                value =>
                {
                    _passed = true;
                }
                ,
                e =>
                {
                    notifyError(e);
                }
                ,
                () =>
                {
                    notifyComplete();
                }
                
                );


            _subscriped = _subject.ColdSubscribe(observer);
        }

        public override void execute()
        {
            try
            {
                _subject.execute();
                notifyValue(_passed);
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }

    }


    public class AverageSubject<T> : BaseSubject<double>
    {
        private BaseSubject<int> _subject;
        private IDisposable _subscriped;

        private double _value = -1;
        private int _count = 0;

        public AverageSubject(IObservable<int> source) 
        {
            _subject = (BaseSubject<int>)source;

            var observer = new Observer<int>(
                v =>
                {
                    _value = _value + v;
                    _count++;
                }
                ,
                e =>
                {
                    notifyError(e);
                }
                ,
                () =>
                {
                    notifyComplete();
                }

                );

            _subscriped = _subject.ColdSubscribe(observer);

        }

        public override void execute()
        {
            try
            {
                _subject.execute();
                double average = this._value / _count;
                notifyValue(average);
                notifyComplete();
            }
            catch (Exception e)
            {
                notifyError(e);
            }
        }

    }

    public class Allsubject<T> : BaseSubject<bool>
    {
        private IDisposable _subscriped;
        private BaseSubject<T> _subject;
        private Func<T, bool> _predicate;
        private bool passed = true;


        public Allsubject(IObservable<T> source, Func<T, bool> predicate)
        {
            _subject = (BaseSubject<T>)source;
            _predicate = predicate;

            var observer = new Observer<T>(

                v =>
                {
                    if (!predicate(v))
                    {
                        passed = false;
                    }
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {

                }
                );

            _subscriped = _subject.ColdSubscribe(observer);
            
        }

        public override void execute()
        {
            try
            {
                _subject.execute();
                notifyValue(passed);
                notifyComplete();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }
    }


    public class AggregateSubject<T> : BaseSubject<T>
    {
        private BaseSubject<T> _subject;
        private IDisposable _subscriped;
        private Func<T, T, T> _accumulator;
        private T _value;
        private bool valueIsSet = false;
       // private ThreadExecuter threadExecuter;

        public AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        public AggregateSubject(IObservable<T> source, Func<T, T, T> accumulator, IScheduler scheduler)
        {
            if(_Scheduler == null)
            {
                //_Scheduler = new CurrentThreadScheduler();
            }
            else
            {
                _Scheduler = scheduler;
            }
            
            _subject = (BaseSubject<T>)source;
            _accumulator = accumulator;
            var observer = new Observer<T>(
                v =>
                {
                    if(!valueIsSet)
                    {
                        valueIsSet = true;
                        _value = _accumulator(v, default(T));
                    }
                    else
                    {
                        _value = _accumulator(v, _value);
                    }
                    
                    
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    OnComplete();
                }
                );

            _subscriped = _subject.ColdSubscribe(observer);
        }

        private void OnComplete()
        {
            autoResetEvent.Set();
        }

        public override void execute()
        {
            try
            {
                _subject.execute();
                WaitHandle.WaitAll(new WaitHandle[] { autoResetEvent });
                notifyValue(_value);
                notifyComplete();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }
    }




    public class CastSubject<T> : BaseSubject<T>
    {
        private BaseSubject<object> _subject;

        private IDisposable _subscriped;

        public CastSubject(IObservable<object> source)
        {
            _subject = (BaseSubject<object>)source;

            var observer = new Observer<object>(
                v =>
                {
                    T castValue = (T)v;
                    notifyValue(castValue);
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }

                );

            _subscriped = _subject.ColdSubscribe(observer);
        }


        public override void execute()
        {
            try
            {
                _subject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }
    }


    public class CreateSubject<T> : BaseSubject<T>
    {
        private Func<IObserver<T>, Action> _subscribe;

        public CreateSubject(Func<IObserver<T>, Action> subscribe)
        {
            _subscribe = subscribe;
        }

        public override void execute()
        {
            IObserver<T> observer = new Observer<T>(
                o =>
                {
                    foreach (var i in _observers)
                    {
                        i.OnNext(o);
                    }
                },
                e =>
                {
                    foreach(var i in _observers)
                    {
                        i.OnError(e);
                    }
                },
                () =>
                {
                    foreach(var i in _observers)
                    {
                        i.OnCompleted();
                    }
                }
                );

            Action action = _subscribe(observer);
            
        }

    }


    public class ConcatSubject<T> : BaseSubject<T>
    {
        private BaseSubject<T> _fstSubject;
        private BaseSubject<T> _sndSubject;

        public ConcatSubject(IObservable<T> fstsource, IObservable<T> sndSource)
        {
            _fstSubject = (BaseSubject<T>)fstsource;
            _sndSubject = (BaseSubject<T>)sndSource;

            var observer = new Observer<T>(
                v =>
                {
                    notifyValue(v);
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    var ob2 = new Observer<T>(
                        v2 =>
                        {
                            notifyValue(v2);
                        },
                        ex =>
                        {
                            notifyError(ex);
                        },
                        () =>
                        {
                            notifyComplete();
                        }
                        );
                    _sndSubject.Subscribe(ob2);
                }
                );
            _fstSubject.ColdSubscribe(observer);
        }

        public override void execute()
        {
            try
            {
                _fstSubject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }

    }
    

    public class StartWithSubject<T> : BaseSubject<T>
    {
        private BaseSubject<T> _subject;

        private IDisposable _subscriped;

        private T[] _values;


        public StartWithSubject(IObservable<T> source, T[] values)
        {
            _values = values;
            _subject = (BaseSubject<T>)source;

            var observer = new Observer<T>(
                v =>
                {
                    notifyValue(v);
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }
                );
            _subscriped = _subject.ColdSubscribe(observer);
        }

        public override void execute()
        {
            try
            {
                foreach (var v in _values)
                {
                    notifyValue(v);
                }
                _subject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
            }

        }
    }


    public class ScanSubject<T> : BaseSubject<T>
    {
        private Func<T, T, T> _accumulator;
        private IDisposable _subscriped;
        private BaseSubject<T> _subject;
        private T _accumulatorValue = default(T);

        public ScanSubject(IObservable<T> source, Func<T, T, T> accumulattor, T seed)
        {
            _subject = (BaseSubject<T>)source;
            _accumulator = accumulattor;
            _accumulatorValue = seed;
            var observer = new Observer<T>(
                v =>
                {
                    _accumulatorValue = _accumulator(v, _accumulatorValue);
                    notifyValue(_accumulatorValue);
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }
                );
            _subscriped = _subject.ColdSubscribe(observer);
        }


        public override void execute()
        {
            try
            {
                _subject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }
    }


    public class MapSubject<T, TResult> : BaseSubject<TResult>
    {
        private BaseSubject<T> _subject;

        private Func<T, TResult> _mapFun;

        public  MapSubject(IObservable<T> source, Func<T, TResult> mapFun)
        {
            _subject = (BaseSubject<T>)source;
            _mapFun = mapFun;

            var observer = new Observer<T>(
                v =>
                {
                    TResult r = _mapFun(v);
                    notifyValue(r);
                },
                e =>
                {
                    notifyError(e);
                },
                () =>
                {
                    notifyComplete();
                }
                );
        }

        public override void execute()
        {
            try
            {
                _subject.execute();
            }
            catch(Exception e)
            {
                notifyError(e);
            }
        }
        
    }


    // xxxsubject
    public static class Observable
    {
        public static IObservable<T> Return<T>(T v)
        {
            return new ReturnSubject<T>(v);
        }

        public static IObservable<T> Return<T>(T[] v)
        {
            return new ReturnSubjectArray<T>(v);
        }

        public static IObservable<T> Return<T>(IEnumerable<T> v)
        {
            return new ReturnSubjectCollection<T>(v);
        }

        public static IObservable<T> Return<T>(List<T> v)
        {
            return new ReturnSubjectCollection<T>(v);
        }
        public static IObservable<int> Range(int start, int xtime)
        {
            return new RangeSubject(start, xtime);
        }

        public static IObservable<T> Empty<T>()
        {
            return new EmptySubject<T>();
        }

        public static IObservable<T> Repeat<T>(T value)
        {
            return new RepeatSubject<T>(value);
        }

        public static IObservable<T> Throw<T>(Exception e)
        {
            return new ThrowSubject<T>(e);
        }

        public static IObservable<T> Never<T>()
        {
            return new NeverSubject<T>();
        }

        public static IObservable<T> ObserveOn<T>(this IObservable<T> source, IScheduler scheduler)
        {
            if (scheduler == null)
            {
                return new ObserveOnSubject<T>(source);
            }
            else
            {
                return new ObserveOnSubject<T>(source, scheduler);
            }

        }

        //public static IObservable<int> Min(this IObservable<int> source)
        //{
        //    return new MinSubjectInt(source);
        //}

        public static IObservable<T> Min<T>(this IObservable<T> source) where T : IComparable<T>
        {
            return new MinSubject<T>(source);
        }


        public static IObservable<T> Distinct<T>(this IObservable<T> source)
        {
            return new DistinctSubject<T>(source);
        }

        public static IObservable<T> ElemAt<T>(this IObservable<T> source, int index)
        {
            return new ElementAtSubject<T>(source, index);
        }

        public static IObservable<T> ElemAt2<T>(this IObservable<T> source, int index)
        {
            return new ElementAtSubject<T>(source, index);
        }

        public static IObservable<T> Generate<T>(T initValue,
                                              Predicate<T> condition,
                                             Func<T, T> iterate,
                                             Func<T, T> resultSelector,
                                             IScheduler scheduler)
        {
            return new GenerateSubject<T>(initValue,
                                       condition,
                                       iterate,
                                       resultSelector,
                                       scheduler);
        }



        public static IObservable<IList<T>> Buffer<T>(this IObservable<T> source, int count)
        {
            var bufferSubject = new BufferSubject<IList<T>, T>(count);
            bufferSubject.Start(source);
            return bufferSubject;
        }


        public static IObservable<T> Delay<T> (this IObservable<T> source, TimeSpan timespan)
        {
            return new DelaySubject<T>(source, timespan);
        }

        public static IObservable<T> Catch<T>(this IObservable<T> fst, IObservable<T> snd)
        {
            return new CatchSubject<T>(fst, snd);
        }


        public static IObservable<T> Finally<T> (this IObservable<T> source, Action finallyAction)
        {
            return new FinallySubject<T>(source, finallyAction);
        }

        public static IObservable<double> Average(this IObservable<int> source)
        {
            return new AverageSubject<int>(source);
        }

        public static IObservable<bool> All<T>(this IObservable<T> source, Func<T, bool> predicate)
        {
            return new Allsubject<T>(source, predicate);
        }

        public static IObservable<T> Aggregate<T> (this IObservable<T> source, Func<T, T, T> accmulator)
        {
            return new AggregateSubject<T>(source, accmulator, null);
        }


        public static IObservable<TResult> Cast<TResult>(this IObservable<object> source)
        {
            return new CastSubject<TResult>(source);
        }

        public static IObservable<T> Create<T>(Func<IObserver<T>, Action> subscribe)
        {
            return new CreateSubject<T>(subscribe);
        }


        public static IObservable<T> Concat<T> (this IObservable<T> firstsource, IObservable<T> sndsource)
        {
            return new ConcatSubject<T>(firstsource, sndsource);
        }

        public static IObservable<T> StartWith<T>(this IObservable<T> source, params T[] values)
        {
            return new StartWithSubject<T>(source, values);
        }

        public static IObservable<T> Scan<T>(this IObservable<T> source, T seed, Func<T, T, T> accumulatior)
        {
            return new ScanSubject<T>(source, accumulatior, seed);

        }

        public static IObservable<TResult> Map<T, TResult>(this IObservable<T>source , Func<T, TResult>mapFun)
        {
            return new MapSubject<T, TResult>(source, mapFun);
        }




    }//Observable




    public static class ObservableExtension
    {
        public static IObservable<T> ToObservable<T>(this T value)
        {
            return new ReturnSubject<T>(value);
        }

        public static IObservable<T> ToObservable<T>(this T[] array)
        {
            return new ReturnSubjectArray<T>(array);
        }

        public static IObservable<T> ToObservable<T>(this List<T> collection)
        {
            return new ReturnSubjectCollection<T>((IEnumerable<T>)collection);
        }

        public static IObservable<T> ToObservable<T>(this IEnumerable<T> collection)
        {
            return new ReturnSubjectCollection<T>(collection);
        }
        public static IObservable<T> ToEmpty<T>(this T value)
        {
            return new EmptySubject<T>();
        }


        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext, Action<Exception> onErr, Action onComplete)
        {
            Observer<T> observer = new Observer<T>(onNext, onErr, onComplete);
            IDisposable subject = observable.Subscribe(observer);
            return subject;
        }


        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext, Action<Exception> onErr)
        {
            Observer<T> observer = new Observer<T>(onNext, onErr);
            IDisposable subject = observable.Subscribe(observer);
            return subject;
        }


        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext)
        {
            Observer<T> observer = new Observer<T>(onNext);
            IDisposable subject = observable.Subscribe(observer);
            return subject;
        }


    }


    public class Program
    {
        static void Main(string[] args)
        {

            /*
                        9.ToObservable().Subscribe(
                            value => Console.WriteLine("observer running: the value is {0} ", value)
                            );
                        Observable.Return(10).Subscribe(
                            value => Console.WriteLine("observer running: the value is {0} ", value)
                            );
                          
            Observable.Range(10, 2).Subscribe(
                            v => Console.WriteLine(v)
                            );
                              */

            /*
            Observable.Empty<String>().Subscribe(
                v =>
                {
                    if (v == null)
                        Console.WriteLine("v is null");
                    else
                        Console.WriteLine("v is not null");
                }
                );
            Observable.Empty<char>().Subscribe(
                v =>
                {
                    if (v == 0)
                        Console.WriteLine("v is 0");
                    else
                        Console.WriteLine("v is not null");
                }
                );
                */

            /*
            "aa".ToEmpty().Subscribe(
               v =>
               {
                   if (v == null)
                       Console.WriteLine("v is null");
                   else
                       Console.WriteLine("v is not null");
               }
               );
            '1'.ToEmpty().Subscribe(
               v =>
               {
                   if (v == 0)
                       Console.WriteLine("v is 0");
                   else
                       Console.WriteLine("v is not null");
               }
               );
                */


            /*
            int[] numbers; // declare numbers as an int array of any size
            numbers = new int[10];
            for(var i = 0; i < numbers.Count(); i++)
            {
                numbers[i] = (i+1) * 7;
            }
            Observable.Return(numbers).Subscribe(
                               v =>
                               {
                                   Console.WriteLine(v);
                               }
                               
                );
            numbers.ToObservable().Subscribe(
                                               v =>
                                               {
                                                   Console.WriteLine(v);
                                               }
                );
            */


            /*
            List<string> t10 = new List<string>() { "i1", "i3", "i4", "i6" };
            Observable.Return(t10).Subscribe(
                               v =>
                               {
                                   Console.WriteLine(v);
                               }
                );
            t10.ToObservable().Subscribe(
                                               v =>
                                               {
                                                   Console.WriteLine(v);
                                               }
                );
    */




            //Console.WriteLine("main thread id is {0}" ,Thread.CurrentThread.ManagedThreadId.ToString());
            //Console.Read();
            //Observable.Repeat("hi\n").Subscribe(

            //                                                   v =>
            //                                                   {
            //                                                       Console.WriteLine(v );
            //                                                       Console.WriteLine(Thread.CurrentThread.ManagedThreadId.ToString());
            //                                                   }
            //    );


            /* 
            Observable.Throw<object>(new Exception("custom exception")).Subscribe(
                x =>
                {
                    var y = x;
                } ,
                e =>
                {
                    Console.WriteLine(e.Message);
                });
                */


            /* 
            Observable.Never<object>().Subscribe(
                x =>
                {
                    Console.WriteLine("this will never show");
                }
                );
                */



            //Observable.Return<int>(42).ObserveOn(null).Subscribe<int>(
            //    x => Console.WriteLine(x),
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));


            //Observable.Range(10, 2).Min().Subscribe(
            //     v => Console.WriteLine(v)
            //     );
            // !!!!!!!!!!!!!!!!!! error  Min return 0, because Range use thread to generate num,  when  notifyValue(_minValue) execute , the range haven't execute at all, so it always return 0;

            //Observable.Range(10, 2).ObserveOn(new CurrentThreadScheduler()).Min().Subscribe(
            //v => Console.WriteLine(v)
            //);

            /*
                        int[] numbers; // declare numbers as an int array of any size
                        numbers = new int[10];
                        for (var i = 0; i < numbers.Count(); i++)
                        {
                            numbers[i] = (i + 1) * 7;
                        }
                        numbers.ToObservable().Min().Subscribe(
                                                           v =>
                                                           {
                                                               Console.WriteLine(v);
                                                           }
                            );
                            */



            //int[] numbers; // declare numbers as an int array of any size
            //numbers = new int[10];
            //for (var i = 0; i < numbers.Count(); i++)
            //{
            //	numbers[i] = (i + 1) * 7;
            //             if(i > 4){
            //                 numbers[i] = 7;
            //             }
            //}
            //        numbers.ToObservable().Distinct().Subscribe(
            //							   v =>
            //							   {
            //								   Console.WriteLine(v);
            //							   }
            //);


            //int[] numbers; // declare numbers as an int array of any size
            //numbers = new int[10];
            //for (var i = 0; i < numbers.Count(); i++)
            //{
            //  numbers[i] = (i + 1) * 7;

            //}
            //         numbers.ToObservable().ElemAt(2).Subscribe(
            //                             v =>
            //                             {
            //                                 Console.WriteLine(v);
            //                             }
            //);
            //Observable.Generate(1, i => i <= 5, i => i + 1, i => i + 100, null).Subscribe(
            //    v =>
            //    {
            //        Console.WriteLine(v);
            //    }
            //);



            //IObservable<IList<int>> returnObject = Observable.Generate(1, i => i <= 5, i => i + 1, i => i, null).Buffer(19);
            //returnObject.Subscribe<IList<int>>(
            //        x => 
            //    {
            //        foreach(var i in x)
            //        {
            //            Console.WriteLine(i);
            //        }
            //        //Console.WriteLine(x);
            //    },
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));


            //Observable.Range(1, 4).Delay(TimeSpan.FromSeconds(3)).Subscribe(
            //    v =>
            //    {
            //        Console.WriteLine(v);
            //    }

            //    );

            //Observable.Throw<int>(new Exception("aa")).Catch(Observable.Return(10)).Subscribe<int>(
            //   x => { Console.WriteLine(x); },
            //   ex => Console.WriteLine("OnError {0}", ex.Message),
            //   () => Console.WriteLine("OnCompleted"));



            //Observable.Throw<int>(new Exception("aa")).Finally(() => { Console.WriteLine("finally!"); }).Subscribe<int>(
            //   x => { Console.WriteLine(x); },
            //   ex =>
            //   {
            //       //Console.WriteLine("OnError {0}", ex.Message);
            //   },
            //   () => Console.WriteLine("OnCompleted"));


            //Observable.Generate(1, i => i <= 5, i => i + 1, i => i, null).Average().Subscribe<double>
            // (
            //    x =>
            //    {
            //        Console.WriteLine(x);
            //    }

            //    );


            //Observable.Generate(1, i => i <= 5, i => i + 1, i => i, null).All(a => a > 0).Subscribe<bool>(
            //    x => Console.WriteLine(x),
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));



            //IObservable<int> generateObject = Observable.Generate(0, i => i < 3, i => i + 1, i => i, null).Aggregate((x, y) =>
            //{
            //   Thread.Sleep(1000);
            //   return  x + y;
            //});
            //generateObject.Subscribe<int>(
            //    x =>
            //    {
            //        Console.WriteLine(x);
            //    },
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));


            //object obj = (object)2.2;
            //IObservable<double> returnObject = Observable.Return(obj).Cast<double>();
            //returnObject.Subscribe<double>(
            //    x => { Console.WriteLine(x); },
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));



            //var generatedObject = Observable.Create<int>(o =>
            //{
            //    o.OnNext(10);
            //    o.OnNext(20);
            //    return () => { };
            //});
            //IDisposable disposable = generatedObject.Subscribe(
            //    x =>
            //    {
            //        Console.WriteLine(x) ;
            //    },
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));
            //disposable.Dispose();


            //IObservable<int> returnObject = Observable.Range(1, 4).Concat(Observable.Range(15, 5));
            //returnObject.Subscribe<int>(
            //    x => {  Console.WriteLine(x); },
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));




            //IObservable<int> returnObject = Observable.Generate(0, i => i < 10, i => i + 1, i => i, null).StartWith(20, 21, 22, 23, 24);
            //returnObject.Subscribe<int>(
            //    x => { Console.WriteLine(x); },
            //    ex => Console.WriteLine("OnError {0}", ex.Message),
            //    () => Console.WriteLine("OnCompleted"));






            //   while (true)
            {
                var r = Console.ReadLine();
                Console.WriteLine(r);
            }

        }
    }
}