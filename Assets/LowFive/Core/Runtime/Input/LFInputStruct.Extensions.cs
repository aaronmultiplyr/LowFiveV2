// Assets/LowFive/Core/Runtime/Input/LFInputStruct.Extensions.cs
// Friendly extension methods that map specific buttons to bit indices.
//
// bit 0 = W   (forward)
// bit 1 = S   (back)
// bit 2 = A   (left)
// bit 3 = D   (right)
// bit 4 = LMB (fire / jump)

using LowFive.Core.Input;

namespace LowFive.Core.Input
{
    public static class LFInputStructExt
    {
        public static bool W(this in LFInputStruct i) => i.GetButton(0);
        public static bool S(this in LFInputStruct i) => i.GetButton(1);
        public static bool A(this in LFInputStruct i) => i.GetButton(2);
        public static bool D(this in LFInputStruct i) => i.GetButton(3);
        public static bool LMB(this in LFInputStruct i) => i.GetButton(4);
    }
}
