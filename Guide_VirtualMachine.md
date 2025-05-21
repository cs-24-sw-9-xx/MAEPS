# Guide for running the simulation in a virtual machine
This guide describes how to run the simulation in a virtual machine.
The guide is divided into the following sections:
- [Virtual Machine Setup in AAU Strato](#virtual-machine-setup-in-aau-strato)
  - [Launch an instance](#launch-an-instance)
  - [Connect to the instance launch in AAU Strato via SSH](#connect-to-the-instance-launch-in-aau-strato-via-ssh)
  - [Initial setup of the instance](#initial-setup-of-the-instance)
  - [Download the latest build of StandaloneLinux64-Server](#download-the-latest-build-of-standalonelinux64-server)
  - [Run experiments](#run-experiments)

This guide describes how to setup a virtual machine in AAU Strato. If you are using another cloud provider, and the instance runs Linux, and you can ssh into the instance, you can just follow the steps from the section "Initial setup of the instance" and onwards.

## Virtual Machine Setup in AAU Strato
Follow the guide in the link below to authenticate to Openstack dashboard (where the Strato instances are launched) and to do some initial Openstack setup.
Please follow the guide from the beginning until the section "Launch Ubuntu instance", and remember to do all the steps.

The guide is available at: https://hpc.aau.dk/strato/getting-started/launch-instance/#initial-openstack-setup

### Launch an instance
1. Navigate to `Compute -> Images`. You should see the following:
![Screenshot 2025-05-15 at 10.37.35.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2010.37.35.png)

2. Then click on the button `Launch Instance` (Placed at the top right). You should see the following:
![Screenshot 2025-05-15 at 10.44.11.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2010.44.11.png)

3. Give a name to the instance. In this guide the name of the instance is "Server", but you can freely type another name for it.
4. Click on the `Source` to the left. You should see the following:
![Screenshot 2025-05-15 at 10.48.30.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2010.48.30.png)
5. Type a volume size for you instance. In this example a 1000GB volume size is used, but you can freely type. The maximum volume size is #####.
6. Choose the OS you want the instance to run. In this example `Arch Linux Latest` is used, but you can freely use any other Linux distributions.
7. You should end up with this:
![Screenshot 2025-05-15 at 10.55.40.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2010.55.40.png)
8. Click on the `Flavour` to the left. You should see the following:
![Screenshot 2025-05-15 at 10.57.16.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2010.57.16.png)
9. To the instance you want to use. In this example the highest VCPUs and RAM is selected. You screen should look like this:
![Screenshot 2025-05-15 at 10.59.01.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2010.59.01.png)
10. Click on the `Networks` to the left. You should see the following:
![Screenshot 2025-05-15 at 11.00.14.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2011.00.14.png)
11. If the instance should only be access at the Aalborg University, select `Campus Network 01` or `Campus Network 02`. In this example `AAU Public 2` is selected, because then the instance can be access outside the university, and sometimes `AAU Public 1` does not work. You screen should look like this:
![Screenshot 2025-05-15 at 11.05.11.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2011.05.11.png)
12. Click on the `Key Pair` to the left. Check that the Key Pair you added in [Virtual Machine Setup in AAU Strato](#Virtual-Machine-Setup-in-AAU-Strato) is set under the allocated. In this example the name of the Key Pair is "Server". You should see the following:
![Screenshot 2025-05-15 at 15.18.16.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2015.18.16.png)
13. Now the instance is ready to be launched. Click on the `Launch Instance` button at the bottom right. You should then see the following:
![Screenshot 2025-05-15 at 15.18.28.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2015.18.28.png)
14. After refreshing the page several times, you should see the instance you just launched is ready. You should see the following:
![Screenshot 2025-05-15 at 15.19.08.png](.readmeGuideVMAsserts/Screenshot%202025-05-15%20at%2015.19.08.png)

## Connect to the instance launch in AAU Strato via SSH
The guide in the link below describes how to connect to the instance via SSH:

https://hpc.aau.dk/strato/getting-started/access-instance/

You connection should have format like this:
```bash
ssh -i <path_to_ssh_key> <username>@<ip_address>
```

| Parameter         | Description                                                              |
|-------------------|--------------------------------------------------------------------------|
| <path_to_ssh_key> | The path to the SSH key                                                  |
| \<username\>      | The username to your instance. If the OS is Arch, the username is `arch` |
| <ip_address>      | the IP address of the instance you just launched. You can find the IP address in the Openstack dashboard under `Compute -> Instances`.                                                                         |

## Initial setup of the instance
| Package    | Reason                                                                          |
|------------|---------------------------------------------------------------------------------|
| `curl`     | Used to download the artifact                                                   |
| `jq`       | Used to parse and extract data from the JSON response returned by GitHub's API  |
| `unzip`    | Used to unzip the artifact                                                      |
| `tmux`     | Used to run the experiments in background                                       |
| `moreutils`| Used `ts` for logging                                                           |

1. Run the following command to install the required packages in Arch. If using other the Linux distribution, please install the required packages using the package manager of the Linux distribution.
```bash
sudo pacman -Sy --noconfirm curl jq unzip tmux moreutils
```

## Download the latest build of StandaloneLinux64-Server
1. Move 'download-Build-StandaloneLinux64-Server.sh' from your local computer to the instance. You can use `scp` to move the file to the instance. The command should look similar to this:
```bash
scp -i <path_to_ssh_key> ./path-to/download-Build-StandaloneLinux64-Server.sh <username>@<ip_address>:~/   
```
| Parameter         | Description                                                              |
|-------------------|--------------------------------------------------------------------------|
| <path_to_ssh_key> | The path to the SSH key                                                  |
| \<username\>      | The username to your instance. If the OS is Arch, the username is `arch` |
| <ip_address>      | the IP address of the instance you just launched. You can find the IP address in the Openstack dashboard under `Compute -> Instances`.                                                                         |

2. Open Terminal and SSH into the instance.

3. Set the environment variables needed before running the script.
It can be beneficial to add the environment variables to your `~/.bashrc` file, so you do not have to set them every time you start a new terminal session.
The following environment variables are needed:
```bash
export GITHUB_TOKEN="your_github_token_here"
export GITHUB_OWNER="your_github_username_or_org"
export REPO_NAME="your_repository_name"
```

4. Run the script to download the latest build of StandaloneLinux64-Server:
```bash
chmod +x download-Build-StandaloneLinux64-Server.sh // Only needed to run to give permission to run the script.
./download-Build-StandaloneLinux64-Server.sh
```
5. After the script has finished running, you can now navigate to the folder where the run-headless.sh script is located. Use the following command:
```bash
cd artifacts/Build-StandaloneLinux64-Server/StandaloneLinux64
```

## Run experiments
1. (OPTIONAL) Before running the experiment, use tmux to create a new session. 
This is optional, but it is recommended to use tmux, so you can detach from the session and leave it running in the background.
To create a new tmux session, use the following command:
```bash
tmux new -s <session_name>
```
Here `<session_name>` is the name of the tmux session you want to create. You can name it whatever you want.

To detach from the tmux session, you can
```bash
1. Hold down Control (Ctrl) and press b.
2. Release both.
3. Press d.
```

To attach to the tmux session again, you can use the following command:
```bash
tmux attach -t <session_name>
```

2. To run the experiment in headless mode, you can use the following command:
```bash
chmod +x run-headless.sh # Only needed to run to give permission to run the script.
./run-headless.sh <EXPERIMENT> <INSTANCES>
```
Here `<EXPERIMENT>` is the name of the experiment you want to run, and `<INSTANCES>` is the number of instances you want to run.
`<EXPERIMENT>` should be in the format `<Namespace>.<ExperimentName>`, where `<Namespace>` is the namespace of the experiment relative to `Maes.Experiments`, and `<ExperimentName>` is the name/class of the experiment.

Example: To run the experiment `HeuristicConscientiousReactiveExperiment` located in the namespace `Maes.Experiments.Patrolling` with 2 instances, you can use the following command:
```bash
./run-headless.sh Patrolling.HeuristicConscientiousReactiveExperiment 2
```

3. After the experiment is completed, the output.log contains all logs, and failure.log contains the names of any scenarios, that did not complete in the given amount of ticks. (default being DefaultMaxLogicTicks)
