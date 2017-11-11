using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicFlow;
using System.Collections.Generic;

namespace LogicFlow.Tests
{
    [TestClass]
    public class LogicFlowTest
    {
        [TestMethod]
        public void BasicDoFlowTest()
        {
            var t_Number = 0;
            new Flow().Do(() => t_Number = 10).ExecuteAsync().Wait();

            Assert.IsTrue(t_Number == 10);
        }

        [TestMethod]
        public void BasicDoUntilFlowTest()
        {
            var t_Number = 0;
            new Flow().DoUntil(() => t_Number++, () => t_Number == 15).ExecuteAsync().Wait();

            Assert.IsTrue(t_Number == 15);
        }

        [TestMethod]
        public void BasicDoUntilWithWhenConditionFlowTest()
        {
            var t_Number = 0;
            new Flow()
                .DoUntil(() => t_Number += 10, () => t_Number >= 100)
                .DoWhen(() => t_Number = -1, () => t_Number >= 100)
                .ExecuteAsync()
                .Wait();

            Assert.IsTrue(t_Number == -1);
        }

        [TestMethod]
        public void BasicDoUntilWithWhenConditionFlowTest2()
        {
            var t_Number = 0;
            new Flow()
                .DoUntil(() => t_Number += 10, () => t_Number >= 200)
                .DoWhen(() => t_Number = -1, () => t_Number <= 100)
                .ExecuteAsync().Wait();

            Assert.IsTrue(t_Number >= 200);
        }

        [TestMethod]
        public void BubbleSortFlowTest()
        {
            var t_ArrayCount = 100;
            var t_Arr = new List<int>();

            for (int i = t_ArrayCount - 1; i >= 0; i--)
                t_Arr.Add(i);

            var t_Switched = false;
            var t_Index = 0;
            new Flow()  .Do(() => t_Switched = false)
                        .DoWhen
                        (
                            () =>
                            {
                                var t_Temp = t_Arr[t_Index + 1];
                                t_Arr[t_Index + 1] = t_Arr[t_Index];
                                t_Arr[t_Index] = t_Temp;
                                t_Switched = true;
                            },
                            () => t_Arr[t_Index] > t_Arr[t_Index + 1]
                        )
                        .Until(() => ++t_Index >= t_Arr.Count - 1)
                        .DoWhen
                        (
                            () => t_Index = 0,
                            () => t_Index + 1 >= t_Arr.Count
                        )
                        .LoopFlowUntil(() => !t_Switched)
                        .ExecuteAsync()
                        .Wait();

            for (int i = 0; i < t_ArrayCount; i++)
                Assert.IsTrue(t_Arr[i] == i);
        }

        [TestMethod]
        public void BubbleSortTraditional()
        {
            var t_ArrayCount = 100;
            var t_Arr = new List<int>();

            for (int i = t_ArrayCount - 1; i >= 0; i--)
                t_Arr.Add(i);

            var t_Switched = false;
            do
            {
                t_Switched = false;
                for (int i = 0; i < t_Arr.Count - 1; i++)
                {
                    if(t_Arr[i] > t_Arr[i + 1])
                    {
                        var t_Temp = t_Arr[i + 1];
                        t_Arr[i + 1] = t_Arr[i];
                        t_Arr[i] = t_Temp;
                        t_Switched = true;
                    }
                }
            }
            while (t_Switched);

            for (int i = 0; i < t_ArrayCount; i++)
                Assert.IsTrue(t_Arr[i] == i);
        }

        [TestMethod]
        public void GoUpAndThenDown()
        {
            var t_Number = -1;
            new Flow() .DoUntil(() => t_Number++, () => t_Number >= 100)
                       .DoUntil(() => t_Number--, () => t_Number == 0)
                       .DoUntil(() => t_Number += 10, () => t_Number == 200)
                       .ExecuteAsync()
                       .Wait();

            Assert.IsTrue(t_Number == 200);
        }

        [TestMethod]
        public void FlowCancelTest()
        {
            var t_Number = 0;
            new Flow()  .Do(() => t_Number++)
                        .CancelFlowWhen(() => t_Number == 150)
                        .LoopFlow()
                        .ExecuteAsync()
                        .Wait();

            Assert.IsTrue(t_Number == 150);
        }

        [TestMethod]
        public void NestedFlowsTest()
        {
            var t_Number = 0;
            var t_Count = 0;
            new Flow()  .Do(() => t_Number = 0)
                        .Do
                        (
                            new Flow()  .Do(() => t_Number++)
                                        .Until(() => t_Number == 100)
                        )
                        .Do(() => t_Count++)
                        .LoopFlowUntil(() => t_Count == 5)
                        .ExecuteAsync()
                        .Wait();

            Assert.IsTrue(t_Count == 5 && t_Number == 100);
        }
    }
}
