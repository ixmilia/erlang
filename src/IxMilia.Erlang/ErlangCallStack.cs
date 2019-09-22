using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Erlang
{
    public class ErlangCallStack
    {
        private Stack<ErlangStackFrame> stack;

        public ErlangCallStack()
        {
            stack = new Stack<ErlangStackFrame>();
            stack.Push(new ErlangStackFrame("", "", 0));
        }

        public ErlangStackFrame CurrentFrame
        {
            get { return stack.Peek(); }
        }

        public int StackDepth
        {
            get { return stack.Count; }
        }

        public void Push(ErlangStackFrame frame)
        {
            stack.Push(frame);
        }

        public ErlangStackFrame Pop()
        {
            return stack.Pop();
        }

        public ErlangStackFrame GetTailCallCandidate(string moduleName, string functionName, int airity)
        {
            foreach (var frame in stack)
            {
                if (frame.Module == moduleName && frame.Function == functionName && frame.Airity == airity)
                    return frame;
            }

            return null;
        }

        public void RewindForTailCall(ErlangStackFrame newFrame)
        {
            bool foundFrame = false;
            int offset = 0;
            foreach (var frame in stack)
            {
                if (ErlangStackFrame.IsTailCallCandidate(frame, newFrame))
                {
                    foundFrame = true;
                    break;
                }

                offset++;
            }

            Debug.Assert(foundFrame); // frame should have been found due to IsTailcallCandidateOnStack()
            for (int i = 0; i <= offset; i++)
            {
                stack.Pop();
            }

            stack.Push(newFrame);
        }
    }
}
