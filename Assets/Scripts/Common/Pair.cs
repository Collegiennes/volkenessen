using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

struct Pair<T, U>
{
    public T First;
    public U Second;

    public Pair(T first, U second)
    {
        First = first;
        Second = second;
    }
}
