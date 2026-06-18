pipeline {
    agent any

    environment {
        VM_IP    = '192.168.83.134'                       // TODO: put your VM's IP here, e.g. 192.168.56.10
        VM_USER  = 'paypoint'
        APP_DIR  = '/home/paypoint/microservice-dashboard'
        GIT_REPO = 'https://github.com/chirumama/microservicetest.git'
        GIT_BRANCH = 'master'
        HEALTH_PORT = '3001'
    }

    stages {

        stage('Clone / Pull') {
            steps {
                withCredentials([
                    sshUserPrivateKey(credentialsId: 'vm-ssh-key', keyFileVariable: 'SSH_KEY')
                ]) {
                    bat """
                        ssh -i "%SSH_KEY%" -o StrictHostKeyChecking=no %VM_USER%@%VM_IP% "if [ -d \\"%APP_DIR%/.git\\" ]; then cd %APP_DIR% && git pull origin %GIT_BRANCH%; else git clone -b %GIT_BRANCH% %GIT_REPO% %APP_DIR%; fi"
                    """
                }
            }
        }

        stage('Build & Deploy') {
            steps {
                withCredentials([
                    sshUserPrivateKey(credentialsId: 'vm-ssh-key', keyFileVariable: 'SSH_KEY')
                ]) {
                    bat """
                        ssh -i "%SSH_KEY%" -o StrictHostKeyChecking=no %VM_USER%@%VM_IP% "cd %APP_DIR% && docker compose down && docker compose up --build -d"
                    """
                }
            }
        }

        stage('Health Check') {
            steps {
                sleep(time: 20, unit: 'SECONDS')
                withCredentials([
                    sshUserPrivateKey(credentialsId: 'vm-ssh-key', keyFileVariable: 'SSH_KEY')
                ]) {
                    bat """
                        ssh -i "%SSH_KEY%" -o StrictHostKeyChecking=no %VM_USER%@%VM_IP% "curl -f http://localhost:%HEALTH_PORT% || exit 1"
                    """
                }
            }
        }
    }

    post {
        success {
            echo 'Deployment successful!'
        }
        failure {
            echo 'Deployment failed — check logs above.'
        }
    }
}
