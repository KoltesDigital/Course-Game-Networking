const go = scene.createGameObject();
go.addComponent(new Renderer());
go.addComponent(new NetworkSynchronization());
go.addComponent(new MouseFollower());
