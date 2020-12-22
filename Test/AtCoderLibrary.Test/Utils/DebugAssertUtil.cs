﻿using System;
using AtCoder.Internal;
using FluentAssertions.Specialized;
#if !DEBUG
using System.Threading.Tasks;
using FluentAssertions;
#endif

namespace AtCoder
{
    public static class DebugAssertUtil
    {
        public static void ThrowDebugAssertIfDebug<TDelegate>(
            this DelegateAssertions<TDelegate> assertions,
            string because = "",
            params object[] becauseArgs) where TDelegate : Delegate
        {
#if DEBUG
            assertions.Throw<DebugAssertException>(because, becauseArgs);
#else
            // timeout if subject has infinite loop
            Func<Task> taskSubject =
                () => Task.WhenAny(Task.Run(() => assertions.Subject.DynamicInvoke()), Task.Delay(100));
            taskSubject.Should().NotThrowAsync<DebugAssertException>(because, becauseArgs);
#endif
        }
    }
}
