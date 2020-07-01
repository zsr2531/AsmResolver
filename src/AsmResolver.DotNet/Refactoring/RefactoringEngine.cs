using System.Collections.Generic;

namespace AsmResolver.DotNet.Refactoring
{
    public sealed class RefactoringEngine
    {
        private readonly Workspace _workspace;
        
        public RefactoringEngine(Workspace workspace)
        {
            _workspace = workspace;
        }

        public Queue<IRefactoring> Refactorings
        {
            get;
        } = new Queue<IRefactoring>();

        public void Enqueue(IRefactoring refactoring)
        {
            Refactorings.Enqueue(refactoring);
        }

        public void Apply(IRefactoring refactoring)
        {
            refactoring.Apply(_workspace);
        }

        public void Undo(IRefactoring refactoring)
        {
            refactoring.Undo(_workspace);
        }

        public void ApplyAll()
        {
            var appliedRefactorings = new Queue<IRefactoring>();

            try
            {
                while (Refactorings.Count > 0)
                {
                    var action = Refactorings.Dequeue();
                    Apply(action);
                    appliedRefactorings.Enqueue(action);
                }
            }
            catch
            {
                while (appliedRefactorings.Count > 0)
                {
                    appliedRefactorings.Dequeue().Undo(_workspace);
                }
            }
        }
    }
}