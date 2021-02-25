using Microsoft.Xrm.Sdk;
using System.Text;

namespace Data
{
    public interface ITasks
    {
        StringBuilder StartUp(IOrganizationService conString);
    }
}
