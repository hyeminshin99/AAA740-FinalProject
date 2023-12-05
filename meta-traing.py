import torch
import torch.nn as nn

# Meta Training Step
def train_meta(model,train_dl,learn):
  opt_cl=torch.optim.Adam(model.parameters(),lr=learn)  # can change to SGD
  loss_1=0.0 # loss visualization of the first step of optimization
  loss_2=0.0 # loss visualization of the second step of optimization
  for a,b in train_dl:
    a=a.float()
    b=b.float()
    output,pred2,pred3,pred4=model(a.cuda())
    loss3=loss_deep1(pred2,pred3,pred4,b.cuda())
    loss_1 = loss_1 + loss3

    # First Step of Multi-scale Optimization
    opt_cl.zero_grad()
    loss3.backward()
    opt_cl.step()

    # Second stage of optimization
    output,_,_,_=model(a.cuda())
    loss_final=loss_fn(output,b.cuda())
    loss_2 = loss_2 + loss_final
    opt_cl.zero_grad()
    loss_final.backward()
    opt_cl.step()
  
  loss_1=loss_1/len(train_dl)
  loss_2=loss_2/len(train_dl)
  return model, loss_1, loss_2