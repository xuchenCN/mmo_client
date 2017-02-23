using System;
namespace TestProto
{
   public abstract class AccountCallback : IMessageCallback
   {
      public abstract void OnLoginResponse(UserLoginResponse response);
      public abstract void OnEnterResponse(ClientCharacterEnterEvent response);

      public void OnItemCreate(ClientItemCreateEvent clientItemCreateEvent) 
      {
         throw new NotImplementedException();
      }
      public void OnItemMove(ClientItemMoveEvent clientItemMoveEvent) 
      {
         throw new NotImplementedException();
      }
      public void OnItemDestroy(ClientItemDestroyEvent clientItemDestroyEvent) 
      { 
         throw new NotImplementedException();
      }
      public void OnCharacterCreate(ClientCharacterCreateEvent clientCharacterCreateEvent)
      { 
         throw new NotImplementedException();
      }
   }
}
