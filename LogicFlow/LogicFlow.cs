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

        // Interface
        public FlowAction(Action Execute)
        {
            m_ExecuteAction = Execute;
        }

        public void Execute()
        {
            if(CanContinue() && m_ContinueConditions.Count > 0)
            {
                IsExecuted = true;
            }
            else if (CanExecute())
            {
                m_ExecuteAction?.Invoke();
            }

            if (m_UntilConditions.Count > 0)
                IsExecuted = UntilSatisfied();
            else
                IsExecuted = true;
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

        public bool IsExecuted { get; set; } = false;
    }

    public class Flow
    {
        // Private
        private List<FlowAction> m_Actions = new List<FlowAction>();
        private FlowAction m_CurrentAction = null;
        private Action m_FlowCompletion = null;
        private FlowCondition m_LoopCondition = new FlowCondition(() => true);

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

        public Flow DoUntil(Action Execute, Func<bool> Condition)
        {
            Do(Execute).Until(Condition);
            return this;
        }

        public Flow DoWhen(Action Execute, Func<bool> Condition)
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

        public Flow CancelWhen(Func<bool> CancelCondition)
        {

            return this;
        }

        public Flow ReturnResult()
        {

            return this;
        }

        public Flow LoopUntil(Func<bool> Condition)
        {
            m_LoopCondition = new FlowCondition(Condition);
            return this;
        }

        public bool IsCompleted { get; private set; } = false;
    }

    public class Foobar
    {
        public Foobar()
        {
            var t_Count = 0;
            var t_Flow = new Flow().Do(() => { t_Count++; })
                                    .Until(() => t_Count > 10)
                                    .OnActionCompletion(() => { t_Count = 1000; })
                                    .Do(() => { t_Count = 0; })
                                    .Do(() => { t_Count += 10; })
                                    .ContinueWhen(() => t_Count == 10)
                                    .OnCompletion(() => { t_Count = 0; })
                                    .ExecuteAsync();


            var t_Arr = new List<int> { 9, 2, 3, 4, 7, 1, 8, 5 };
            var t_Switched = false;
            var t_Index = 0;
            var t_SortFlow = new Flow().Do(() => t_Switched = false)
                                        .DoWhen
                                        (
                                            () =>
                                            {
                                                var t_Temp = t_Arr[t_Index + 1];
                                                t_Arr[t_Index + 1] = t_Arr[t_Index];
                                                t_Arr[t_Index] = t_Temp;
                                                t_Switched = true;
                                                t_Index++;
                                            },
                                            () => t_Arr[t_Index] > t_Arr[t_Index + 1]
                                        )
                                        .DoWhen
                                        (
                                            () => t_Index = 0,
                                            () => t_Index >= t_Arr.Count
                                        )
                                        .CancelWhen(() => !t_Switched);

        }
    }
}