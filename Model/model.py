from torch import Tensor
import re
import torch
import torch.nn as nn


class Upsample(nn.Module):
    """ nn.Upsample is deprecated """
    def __init__(self, scale_factor, mode="bilinear"):
        super(Upsample, self).__init__()
        self.scale_factor = scale_factor
        self.mode = mode
 
    def forward(self, x):
        x = F.interpolate(x, scale_factor=self.scale_factor, mode=self.mode, align_corners=True, recompute_scale_factor=True)
        return x  


class Upsample_meta(nn.Module):
    """ nn.Upsample is deprecated """
    def __init__(self, scale_factor,input_features,output_features, mode="bilinear"):
        super(Upsample_meta, self).__init__()
        self.scale_factor = scale_factor
        self.mode = mode
        self.conv=nn.Conv2d(input_features,output_features,kernel_size=1,stride=1)
    def forward(self, x):
        x = self.conv(x)
        x = F.interpolate(x, scale_factor=self.scale_factor, mode=self.mode, align_corners=True, recompute_scale_factor=True)
        return x  
    

class ResidualBlock(nn.Module):
    expansion = 1

    def __init__(self, inplanes, planes, stride=1, downsample=True, bias=False):
        super(ResidualBlock, self).__init__()
        dim_out = planes
        self.stride = stride
        self.conv1 = nn.Conv2d(inplanes, planes, kernel_size=3, stride=stride,
                               padding=1, bias=bias)
        self.bn1 = nn.BatchNorm2d(planes)
        self.relu = nn.ReLU(inplace=True)
        self.conv2 = nn.Conv2d(dim_out, dim_out, kernel_size=(3, 3),
                               stride=1, padding=1)
        self.bn2 = nn.BatchNorm2d(planes)
        if downsample == True:
            self.downsample = nn.Sequential(
                nn.Conv2d(inplanes, planes, kernel_size=1, stride=stride, bias=bias),
                nn.BatchNorm2d(planes),
            )
        elif isinstance(downsample, nn.Module):
            self.downsample = downsample
        else:
            self.downsample = None

    def forward(self, x):
        residual = x
        x = self.conv1(x)
        x = self.bn1(x)
        x = self.relu(x)
        x = self.conv2(x)
        out = self.bn2(x)
        if self.downsample is not None:
            residual = self.downsample(residual)
        out += residual
        out = self.relu(out)
        return out

class fcn_out(nn.Module):
    def __init__(self,input_channels,upsample_factor):
        super(fcn_out,self).__init__()
        self.conv1=nn.Conv2d(input_channels,64,kernel_size=3,stride=1,padding=3//2,dilation=1)
        self.norm1=nn.BatchNorm2d(64)
        self.upsample=Upsample(upsample_factor)
        self.conv2=nn.Conv2d(64,64,kernel_size=3,stride=1,padding=3//2,dilation=1)
        self.conv3=nn.Conv2d(64,3,kernel_size=1,stride=1,padding=0,dilation=1)
    def forward(self,x):
        x=F.relu(self.norm1(self.conv1(x)))
        x=F.relu(self.conv2(self.upsample(x)))
        x=self.conv3(x)
        return torch.sigmoid(x)

class SRMODEL(nn.Module):
    def __init__(self, num_channels=3):
        super(SRMODEL, self).__init__()

        self.res_conv = ResidualBlock

        self.down1 = self.res_conv(num_channels, 32)
        self.down2 = self.res_conv(32, 64)
        self.down3 = self.res_conv(64, 128)
        self.down4 = self.res_conv(128, 256)
        
        self.bridge = self.conv_stage(256, 256)
        

        self.up4 = self.res_conv(1024//2, 512//2)
        self.up3 = self.res_conv(512//2, 256//2)
        self.up2 = self.res_conv(256//2, 128//2)
        self.up1 = self.res_conv(128//2, 64//2)

        self.trans4 = self.upsample(512//2, 512//2)
        self.trans3 = self.upsample(512//2, 256//2)
        self.trans2 = self.upsample(256//2, 128//2)
        self.trans1 = self.upsample(128//2, 64//2)

        self.conv_last = nn.Sequential(
            nn.Conv2d(64//2, 3, 3, 1, 1),
            nn.Sigmoid()
        )

        self.max_pool = nn.MaxPool2d(2)
        self.fcn3=fcn_out(512*2//2,8)
        self.fcn2=fcn_out(256*2//2,4)
        self.fcn1=fcn_out(128*2//2,2)

        for m in self.modules():
            if isinstance(m, nn.Conv2d) or isinstance(m, nn.ConvTranspose2d):
                if m.bias is not None:
                    m.bias.data.zero_()

    def conv_stage(self, dim_in, dim_out, kernel_size=3, stride=1, padding=1, bias=True):
        return nn.Sequential(
            nn.Conv2d(dim_in, dim_out, kernel_size=kernel_size,
                      stride=stride, padding=padding, bias=bias),
            nn.BatchNorm2d(dim_out),
            nn.LeakyReLU(0.1),
            # nn.ReLU(),
            nn.Conv2d(dim_out, dim_out, kernel_size=kernel_size,
                      stride=stride, padding=padding, bias=bias),
            nn.BatchNorm2d(dim_out),
            nn.LeakyReLU(0.1),
            # nn.ReLU(),
        )

    def upsample(self, ch_coarse, ch_fine):
        return nn.Sequential(
            nn.ConvTranspose2d(ch_coarse, ch_fine, 4, 2, 1, bias=False),
            nn.ReLU()
        )

    def forward(self, x):
        conv1_out = self.down1(x)
        conv2_out = self.down2(self.max_pool(conv1_out))
        conv3_out = self.down3(self.max_pool(conv2_out))
        conv4_out = self.down4(self.max_pool(conv3_out))    # ch = 512  

        # multiscale attention process of the encoder's features
        conv1_out=self.attn1(conv1_out,[conv2_out,conv3_out,conv4_out])
        conv2_out=self.attn2(conv2_out,[conv3_out,conv4_out])
        conv3_out=self.attn3(conv3_out,[conv4_out])

        out = self.bridge(self.max_pool(conv4_out))         # ch = 512  

        out = self.trans4(out)
        out_4 = self.fcn3(torch.cat((out, conv4_out), 1))
        out = self.up4(torch.cat((out, conv4_out), 1))

        out = self.trans3(out)
        out_3 = self.fcn2(torch.cat((out, conv3_out), 1))
        out = self.up3(torch.cat((out, conv3_out), 1))

        out = self.trans2(out)
        out_2 = self.fcn1(torch.cat((out, conv2_out), 1))
        out = self.up2(torch.cat((out, conv2_out), 1))
        
        out = self.up1(torch.cat((self.trans1(out), conv1_out), 1))
        out = self.conv_last(out)

        return out, out_2, out_3, out_4