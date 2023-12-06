import torch
import torch.nn as nn


loss_l1=nn.L1Loss()
cos=nn.CosineSimilarity()

def loss_cosine(input,target):
  sim=cos(input,target)
  sim=sim.mean((1,2))
  return sim.mean()

def loss_deep1(pred2,pred3,pred4,target):
  l1,l2,l3=1,1,1  # change lamdas according to need
  loss2=loss_l1(pred2,target)
  loss3=loss_l1(pred3,target)
  loss4=loss_l1(pred4,target)
  return l1*loss2+l2*loss3+l3*loss4

def loss_fn(output,target):
  loss=loss_l1(output,target)
  return loss  