namespace AsmResolver.DotNet.Refactoring
{
    public interface IRefactoring
    {
        void Apply(Workspace workspace);

        void Undo(Workspace workspace);
    }
}