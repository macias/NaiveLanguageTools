using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calculator
{
    public interface IMathNode
    {
        double Eval();
    }

    class AddNode : IMathNode
    {
        IMathNode sub1, sub2;

        internal AddNode(IMathNode s1, IMathNode s2)
        {
            this.sub1 = s1;
            this.sub2 = s2;
        }
        public double Eval()
        {
            return sub1.Eval() + sub2.Eval();
        }
    }
    class SubNode : IMathNode
    {
        IMathNode sub1, sub2;

        internal SubNode(IMathNode s1, IMathNode s2)
        {
            this.sub1 = s1;
            this.sub2 = s2;
        }
        public double Eval()
        {
            return sub1.Eval() - sub2.Eval();
        }
    }
    class DivNode : IMathNode
    {
        IMathNode sub1, sub2;

        internal DivNode(IMathNode s1, IMathNode s2)
        {
            this.sub1 = s1;
            this.sub2 = s2;
        }
        public double Eval()
        {
            return sub1.Eval() / sub2.Eval();
        }
    }
    class MultNode : IMathNode
    {
        IMathNode sub1, sub2;

        internal MultNode(IMathNode s1, IMathNode s2)
        {
            this.sub1 = s1;
            this.sub2 = s2;
        }
        public double Eval()
        {
            return sub1.Eval() * sub2.Eval();
        }
    }
    class PowNode : IMathNode
    {
        IMathNode sub1, sub2;

        internal PowNode(IMathNode s1, IMathNode s2)
        {
            this.sub1 = s1;
            this.sub2 = s2;
        }
        public double Eval()
        {
            return Math.Pow(sub1.Eval(), sub2.Eval());
        }
    }
  
    class SqrtNode : IMathNode
    {
        IMathNode sub;

        internal SqrtNode(IMathNode s)
        {
            this.sub = s;
        }
        public double Eval()
        {
            
            return Math.Sqrt(sub.Eval());
        }
    }
    
    class CosNode : IMathNode
    {
        IMathNode sub;

        internal CosNode(IMathNode s)
        {
            this.sub = s;
        }
        public double Eval()
        {

            return Math.Cos(sub.Eval());
        }
    }
    
    class SinNode : IMathNode
    {
        IMathNode sub;

        internal SinNode(IMathNode s)
        {
            this.sub = s;
        }
        public double Eval()
        {

            return Math.Sin(sub.Eval());
        }
    }
    
    class NumNode : IMathNode
    {
        double val;

        internal NumNode(double v)
        {
            this.val = v;
        }
        public double Eval()
        {

            return val;
        }
    }

}
