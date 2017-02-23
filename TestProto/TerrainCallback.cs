using System;
namespace TestProto
{
   public abstract class TerrainCallback : IMessageCallback
   {
      public void OnLoginResponse(UserLoginResponse response) { throw new NotImplementedException(); }
      public void OnEnterResponse(ClientCharacterEnterEvent response) { throw new NotImplementedException(); }

      public abstract void OnItemCreate(ClientItemCreateEvent clientItemCreateEvent);
      public abstract void OnItemMove(ClientItemMoveEvent clientItemMoveEvent);
      public abstract void OnItemDestroy(ClientItemDestroyEvent clientItemDestroyEvent);
      public abstract void OnCharacterCreate(ClientCharacterCreateEvent clientCharacterCreateEvent);
   }
}
