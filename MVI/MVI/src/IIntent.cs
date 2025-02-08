using System.Threading.Tasks;

namespace MVI
{
    public interface IIntent
    {
        ValueTask<IMviResult> HandleIntentAsync(IState state);
    }
}