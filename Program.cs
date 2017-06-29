using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        void ScheduleInThread(Action action);
        void ScheduleInThread(Action action, CancellationToken cancellationToken);

        void Schedule<T>(Action<T> action, T value);

        void ScheduleInThread<T>(Action<T> action, T value);

        void ScheduleInThread<T>(Action<T> action, T value, CancellationToken cancellationToken);
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
            action();
        }

        public void Schedule<T>(Action<T> action, T value)
        {
            action(value);
        }

        public void ScheduleInThread(Action action)
        {
            Task.Factory.StartNew(action);
        }

        public void ScheduleInThread(Action action, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(action, cancellationToken);
        }

        public void ScheduleInThread<T>(Action<T> action , T value)
        {
            Action fun = () => { action(value); };
            Task.Factory.StartNew(fun);

            //Action<object> actionObject = (object o) => { action(value); };
            //Task.Factory.StartNew(actionObject, (object)value);
        }

        public void ScheduleInThread<T>(Action<T> action, T value, CancellationToken cancellationToken)
        {
            Action fun = () => action(value);
            Task.Factory.StartNew(fun, cancellationToken);
        }
    }





    public abstract class BaseSubject<T> : IObservable<T>
    {
        private IScheduler _scheduler = new TaskScheduler();

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

            if(!_running)
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

            private int repeatCounter = 0;

            public void Execute()
            {
                Action<int> action = null;

                action =
                    (int value) =>
                    {
                        if (__observers.Count <= 0)
                            return;

                        foreach (var i in __observers)
                            i.OnNext(value);

                        if(repeatCounter < __xtime - 1)
                        {
                            repeatCounter++;
                            __scheduler.Schedule(action, ++value);
                        }
                        else
                        {
                            foreach (var i in __observers)
                                i.OnCompleted();
                        }

                        return;
                    };

                action(__value);
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
        }



    }





    public static class Observable
    {
        public static IObservable<T> Return<T>(T v)
        {
            return new ReturnSubject<T>(v);
        }

        public static IObservable<int> Range(int start, int xtime)
        {
            return new RangeSubject(start, xtime);
        }
    }



    public static class ObservableExtension
    {
        public static IObservable<T>  ToObservable<T>(this T value)
        {
            return new ReturnSubject<T>(value);
        }

        public static IDisposable Subscribe<T> (this IObservable<T> observable, Action<T> onNext, Action<Exception> onErr, Action onComplete)
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
            9.ToObservable().Subscribe(
                value => Console.WriteLine("observer running: the value is {0} ", value)
                );

            Observable.Return(10).Subscribe(
                value => Console.WriteLine("observer running: the value is {0} ", value)
                );

            Observable.Range(10, 2).Subscribe(
                v => Console.WriteLine(v)
                );





        }

      
        public void show()
        {
            Console.WriteLine(1);
        }
    }
}
