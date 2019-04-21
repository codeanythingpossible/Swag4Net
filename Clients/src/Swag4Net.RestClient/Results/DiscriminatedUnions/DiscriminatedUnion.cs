using System;

namespace Swag4Net.RestClient.Results.DiscriminatedUnions
{
    public struct Nothing
    {
    }

    public class DiscriminatedUnion<T1, T2, T3, T4, T5> : DiscriminatedUnion<T1, T2, T3, T4>
    {
        protected readonly T5 _case5;
        protected readonly bool _isCase5;
        
        public DiscriminatedUnion(T1 case1) : base(case1)
        {
        }

        public DiscriminatedUnion(T2 case2) : base(case2)
        {
        }

        public DiscriminatedUnion(T3 case3) : base(case3)
        {
        }

        public DiscriminatedUnion(T4 case4) : base(case4)
        {
        }
        
        public DiscriminatedUnion(T5 case5)
        {
            _case5 = case5;
            _isCase5 = true;
        }
        
        public TResult Match<TResult>(
            Func<T1, TResult> f1,
            Func<T2, TResult> f2,
            Func<T3, TResult> f3,
            Func<T4, TResult> f4,
            Func<T5, TResult> f5,
            Func<TResult> defaultCase = null
        )
        {
            return base.Match(f1, f2, f3, f4, () =>
            {
                if (_isCase5)
                    return f5(_case5);
                
                return HandleDefault(defaultCase);
            });
        }
    }
    
    public class DiscriminatedUnion<T1, T2, T3, T4> : DiscriminatedUnion<T1,T2,T3>
    {
        protected readonly T4 _case4;
        protected readonly bool _isCase4;

        protected DiscriminatedUnion()
        {
            
        }
        
        public DiscriminatedUnion(T1 case1) : base(case1)
        {
            
        }

        public DiscriminatedUnion(T2 case2) : base(case2)
        {
        }

        public DiscriminatedUnion(T3 case3) : base(case3)
        {
        }
        
        public DiscriminatedUnion(T4 case4)
        {
            _case4 = case4;
            _isCase4 = true;
        }
        
        public TResult Match<TResult>(
            Func<T1, TResult> f1,
            Func<T2, TResult> f2,
            Func<T3, TResult> f3,
            Func<T4, TResult> f4,
            Func<TResult> defaultCase = null
        )
        {
            return base.Match(f1, f2, f3, () =>
            {
                if (_isCase4)
                    return f4(_case4);
                
                return HandleDefault(defaultCase);
            });
        }
    }
    
    public class DiscriminatedUnion<T1,T2,T3> : DiscriminatedUnion<T1,T2>
    {
        protected readonly T3 _case3;

        protected readonly bool _isCase3;

        protected DiscriminatedUnion()
        {
            
        }
        
        public DiscriminatedUnion(T1 case1) : base(case1)
        {
            
        }

        public DiscriminatedUnion(T2 case2) : base(case2)
        {
        }

        public DiscriminatedUnion(T3 case3)
        {
            _case3 = case3;
            _isCase3 = true;
        }
        
        public TResult Match<TResult>(
            Func<T1, TResult> f1,
            Func<T2, TResult> f2,
            Func<T3, TResult> f3,
            Func<TResult> defaultCase = null
        )
        {
            return base.Match(f1, f2, () =>
            {
                if (_isCase3)
                    return f3(_case3);
                
                return HandleDefault(defaultCase);
            });
        }
    }
    
    public class DiscriminatedUnion<T1,T2>
    {
        protected readonly T1 _case1;
        protected readonly T2 _case2;

        protected readonly bool _isCase1;
        protected readonly bool _isCase2;

        protected DiscriminatedUnion()
        {
            
        }
        
        public DiscriminatedUnion(T1 case1)
        {
            _case1 = case1;
            _isCase1 = true;
        }

        public DiscriminatedUnion(T2 case2)
        {
            _case2 = case2;
            _isCase2 = true;
        }
        
        public TResult Match<TResult>(
            Func<T1, TResult> f1,
            Func<T2, TResult> f2,
            Func<TResult> defaultCase = null
        )
        {
            if (_isCase1)
                return f1(_case1);

            if (_isCase2)
                return f2(_case2);

            return HandleDefault(defaultCase);
        }

        protected TResult HandleDefault<TResult>(Func<TResult> defaultCase = null)
        {
            if (defaultCase == null)
                throw new Exception("Can't resolve discriminated union case");

            return defaultCase();
        }
    }
}