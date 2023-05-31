/*
 * Copyright (c) Johannes Knittel
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VideoCheckingLib.Utils
{
    public class Fft
    {
        private readonly int _len;
        private readonly int _lenLog;
        private readonly int[] _swapPositions;
        private readonly Complex[] _expFactors;

        public Fft(int len)
        {
            _len = len;
            _lenLog  = (int)Math.Log(_len, 2);
            if (len != (int)Math.Pow(2, _lenLog))
                throw new ArgumentException("length must be power of 2");
            _swapPositions = new int[len];
            for (int i = 1; i < _swapPositions.Length; i++)
            {
                _swapPositions[i] = BitReverse(i, _lenLog);
            }

            var numFactors = 0;
            for (int i = 0; i < _lenLog; i++)
            {
                numFactors += (int)Math.Pow(2, i);
            }

            _expFactors = new Complex[numFactors];
            var expI = 0;
            for (int n = 2; n <= _len; n <<= 1)
            {
                var m = n >> 1;
                for (int k = 0; k < m; k++)
                {

                    var term = -2*Math.PI * k / n;
                    var expF = new Complex(Math.Cos(term), Math.Sin(term));
                    _expFactors[expI] = expF;
                    expI++;
                }
            }
        }
        
        static int BitReverse(int n, int numBits)
        {
            var res = 0;
            for (var i = 0; i < numBits; i++)
            {
                res = (res << 1) | (n & 1);
                n >>= 1;
            }
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]

        public void Compute(Span<Complex> buffer)
        {
            if (buffer.Length != _len)
                throw new ArgumentException(
                    $"provided data must match length specified upon initialization (expected {_len} but got {buffer.Length})");
            
            for (int j = 1; j < buffer.Length; j++)
            {
                var swapPos = _swapPositions[j];
                if(swapPos > j) //don't swap twice
                    (buffer[j], buffer[swapPos]) = (buffer[swapPos], buffer[j]);
            }

            ref var bufferRef = ref buffer[0];
            var expI = 0;
            for (int n = 2; n <= buffer.Length; n <<= 1)
            {
                var m = n >> 1;
                for (int k = 0; k < m; k++) 
                {
                    var expF = _expFactors[expI];
                    expI++;
                    ref var evenPtr = ref Unsafe.Add(ref bufferRef, k);
                    for (int evenIndex = k; evenIndex < buffer.Length; evenIndex += n, evenPtr = ref Unsafe.Add(ref evenPtr, n))
                    {
                        ref var oddPtr = ref Unsafe.Add(ref evenPtr, m);
                        var even = evenPtr;
                        var exp = expF * oddPtr;
                        evenPtr = even + exp;
                        oddPtr = even - exp;

                    }
                }
            }
        }

    }
}
