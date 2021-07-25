using System;
using System.Collections.Generic;
using System.Text;

namespace AGraphicsLibrary
{
    public class CurveHelper
    {
        public static float Interpolate(AampLibraryCSharp.Curve curve, float t)
        {
            switch (curve.CurveType)
            {
                case AampLibraryCSharp.CurveType.Hermit2D:
                    return InterpolateHermite2D(t, curve.NumUses, curve.valueFloats);
                default:
                    return 0.0f;
                    //     throw new Exception($"Unsupported color type! {curve.CurveType}");
            }
        }

        //https://github.com/open-ead/sead/blob/16d150caade87410309acbc04069ec9067c78fd6/modules/src/hostio/seadHostIOCurve.cpp
        static float InterpolateHermite2D(float t, uint numUses, float[] f)
        {
            int n = (int)numUses / 3;
            if (f[0] >= t)
                return f[1];

            if (f[3 * (n - 1)] <= t)
                return f[3 * (n - 1) + 1];

            for (int i = 0; i < n; ++i)
            {
                var j = 3 * i;
                if (f[j + 3] > t)
                {
                    var x = (t - f[j]) / (f[j + 3] - f[j]);
                    return ((2 * x * x * x) - (3 * x * x) + 1) * f[j + 1]  // (2t^3 - 3t^2 + 1)p0
                           + ((-2 * x * x * x) + (3 * x * x)) * f[j + 4]   // (-2t^3 + 2t^2)p1
                           + ((x * x * x) - (x * x)) * f[j + 5]            // (t^3 - t^2)m1
                           + ((x * x * x) - (2 * x * x) + x) * f[j + 2]    // (t^3 - 2t^2 + t)m0
                        ;
                }
            }

            return 0;
        }
    }
}
