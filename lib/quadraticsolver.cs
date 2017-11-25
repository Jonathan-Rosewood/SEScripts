public static class QuadraticSolver
{
    public static int Solve(double a, double b, double c,
                            out double s1, out double s2)
    {
        s1 = default(double);
        s2 = default(double);

        if (a == 0.0)
        {
            // Actually linear
            if (b != 0.0)
            {
                s1 = -c / b;
                return 1;
            }
            else
            {
                // Division by zero...
                return 0;
            }
        }
        else
        {
            // Discriminant
            var disc = b * b - 4.0 * a * c;
            if (disc < 0.0)
            {
                return 0; // Imaginary
            }
            else if (disc == 0.0)
            {
                // Single solution
                s1 = -0.5 * b / a;
                return 1;
            }
            else
            {
                // Two solutions
                var q = (b > 0.0) ? (-0.5 * (b + Math.Sqrt(disc))) : (-0.5 * (b - Math.Sqrt(disc)));
                s1 = q / a;
                s2 = c / q;
                if (s1 > s2)
                {
                    // Swap so s1 <= s2
                    var tmp = s1;
                    s1 = s2;
                    s2 = tmp;
                }
                return 2;
            }
        }
    }
}
