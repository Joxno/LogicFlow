using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogicFlow
{
    public interface IFlowAction
    {
        void Execute();
        bool CanExecute();
        bool CanContinue();
        bool UntilSatisfied();
        bool IsExecuted { get; }
    }

    public class FlowCondition
    {
        // Privates
        private Func<bool> m_Condition = null;

        // Interface
        public FlowCondition(Func<bool> Condition)
        {
            m_Condition = Condition;
        }

        public bool IsSatisfied()
        {
            return m_Condition.Invoke();
        }
    }

    public class FlowAction
    {
        // Privates
        private List<FlowCondition> m_ExecuteConditions = new List<FlowCondition>();
        private List<FlowCondition> m_ContinueConditions = new List<FlowCondition>();
        private List<FlowCondition> m_UntilConditions = new List<FlowCondition>();
        private Action m_ExecuteAction = null;
        private Action m_CompletionAction = null;
        private Flow m_ExecuteFlow = null;

        // Interface
        public FlowAction(Action Execute)
        {
            m_ExecuteAction = Execute;
        }

        public FlowAction(Flow Execute)
        {
            m_ExecuteFlow = Execute;
        }

        public void Execute()
        {
            if(CanContinue() && m_ContinueConditions.Count > 0)
            {
                IsExecuted = true;
            }
            else if (CanExecute())
            {
                if (m_ExecuteFlow != null && !m_ExecuteFlow.IsCompleted)
                    m_ExecuteFlow.Execute();
                else
                    m_ExecuteAction?.Invoke();
            }

            if (m_UntilConditions.Count > 0)
                IsExecuted = UntilSatisfied();
            else
            {
                if(m_ExecuteFlow != null)
                {
                    IsExecuted = m_ExecuteFlow.IsCompleted;
                }
                else
                    IsExecuted = true;
            }
        }

        public bool CanExecute()
        {
            foreach (var t_Condition in m_ExecuteConditions)
                if (!t_Condition.IsSatisfied())
                    return false;
            return true;
        }

        public bool CanContinue()
        {
            foreach (var t_Condition in m_ContinueConditions)
                if (!t_Condition.IsSatisfied())
                    return false;
            return true;
        }

        public bool UntilSatisfied()
        {
            foreach (var t_Condition in m_UntilConditions)
                if (!t_Condition.IsSatisfied())
                    return false;
            return true;
        }

        public void AddExecuteCondition(FlowCondition Condition)
        {
            m_ExecuteConditions.Add(Condition);
        }

        public void AddContinueCondition(FlowCondition Condition)
        {
            m_ExecuteConditions.Add(Condition);
        }

        public void AddUntilCondition(FlowCondition Condition)
        {
            m_UntilConditions.Add(Condition);
        }

        public void SetOnCompletion(Action CompletionExecute)
        {
            m_CompletionAction = CompletionExecute;
        }

        private bool m_Executed = false;
        public bool IsExecuted
        {
            get { return m_Executed; }
            set
            {
                if (value == false && m_ExecuteFlow != null)
                    m_ExecuteFlow.IsCompleted = false;
                m_Executed = value;
            }
        }
    }

    public class Flow
    {
        // Private
        private List<FlowAction> m_Actions = new List<FlowAction>();
        private FlowAction m_CurrentAction = null;
        private Action m_FlowCompletion = null;
        private FlowCondition m_LoopCondition = new FlowCondition(() => true);
        private FlowCondition m_CancelCondition = new FlowCondition(() => false);

        private void _Reset()
        {
            foreach (var t_Action in m_Actions)
                t_Action.IsExecuted = false;
        }

        // Interface
        public void Execute()
        {
            if (!IsCompleted)
            {
                if(m_CancelCondition.IsSatisfied())
                {
                    IsCompleted = true;
                    _Reset();
                    return;
                }

                if (!m_CurrentAction.IsExecuted)
                    m_CurrentAction.Execute();

                if (m_CurrentAction.IsExecuted && 
                    m_CurrentAction != m_Actions[m_Actions.Count - 1])
                    m_CurrentAction = m_Actions[m_Actions.FindIndex(A => A == m_CurrentAction) + 1];
                
                if(m_CurrentAction.IsExecuted && m_CurrentAction == m_Actions[m_Actions.Count - 1])
                {
                    if(m_LoopCondition.IsSatisfied())
                    {
                        m_FlowCompletion?.Invoke();
                        IsCompleted = true;
                    }
                    else
                    {
                        m_CurrentAction = m_Actions[0];
                        _Reset();
                    }

                }
            }
        }

        public async Task ExecuteAsync()
        {
            while (!IsCompleted)
                Execute();
        }

        public Flow Do(Action Execute)
        {
            m_Actions.Add(new FlowAction(Execute));

            if (m_CurrentAction == null)
                m_CurrentAction = m_Actions[0];

            return this;
        }

        public Flow Do(Flow Execute)
        {
            m_Actions.Add(new FlowAction(Execute));

            if (m_CurrentAction == null)
                m_CurrentAction = m_Actions[0];

            return this;
        }

        public Flow DoUntil(Action Execute, Func<bool> Condition)
        {
            Do(Execute).Until(Condition);
            return this;
        }

        public Flow DoUntil(Flow Execute, Func<bool> Condition)
        {
            Do(Execute).Until(Condition);
            return this;
        }

        public Flow DoWhen(Action Execute, Func<bool> Condition)
        {
            Do(Execute).When(Condition);
            return this;
        }

        public Flow DoWhen(Flow Execute, Func<bool> Condition)
        {
            Do(Execute).When(Condition);
            return this;
        }

        public Flow Until(Func<bool> Condition)
        {
            m_Actions[m_Actions.Count - 1].AddUntilCondition(new FlowCondition(Condition));
            return this;
        }

        public Flow When(Func<bool> Condition)
        {
            m_Actions[m_Actions.Count - 1].AddExecuteCondition(new FlowCondition(Condition));
            return this;
        }

        public Flow ContinueWhen(Func<bool> Condition)
        {
            m_Actions[m_Actions.Count - 1].AddContinueCondition(new FlowCondition(Condition));
            return this;
        }

        public Flow OnActionCompletion(Action Execute)
        {
            m_Actions[m_Actions.Count - 1].SetOnCompletion(Execute);
            return this;
        }

        public Flow OnCompletion(Action Execute)
        {
            m_FlowCompletion = Execute;
            return this;
        }

        public Flow Branch(Flow F1, Flow F2)
        {

            return this;
        }

        public Flow CancelFlowWhen(Func<bool> CancelCondition)
        {
            m_CancelCondition = new FlowCondition(CancelCondition);
            return this;
        }

        public Flow LoopFlow()
        {
            m_LoopCondition = new FlowCondition(() => false);
            return this;
        }

        public Flow LoopFlowUntil(Func<bool> Condition)
        {
            m_LoopCondition = new FlowCondition(Condition);
            return this;
        }

        private bool m_Completed = false;
        public bool IsCompleted
        {
            get { return m_Completed; }
            set
            {
                if (value == false)
                    _Reset();

                m_Completed = value;
            }
        }
    }
}