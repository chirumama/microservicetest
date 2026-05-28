pipeline {
    agent any

    environment {
        SERVER_IP = '3.110.46.238'
        APP_DIR   = '/home/ubuntu/Microservice_Dash'
    }

    stages {

        stage('Clone / Pull') {
            steps {
                withCredentials([sshUserPrivateKey(credentialsId: 'ubuntu-server-ssh', keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        ssh -i $SSH_KEY -o StrictHostKeyChecking=no ubuntu@${SERVER_IP} '
                            if [ -d "${APP_DIR}/.git" ]; then
                                cd ${APP_DIR} && git pull origin main
                            else
                                git clone https://github.com/paypoint/MicroserviceDashboard.git ${APP_DIR}
                            fi
                        '
                    """
                }
            }
        }

        stage('Build & Deploy') {
            steps {
                withCredentials([sshUserPrivateKey(credentialsId: 'ubuntu-server-ssh', keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        ssh -i $SSH_KEY -o StrictHostKeyChecking=no ubuntu@${SERVER_IP} '
                            cd ${APP_DIR}
                            docker compose down
                            docker compose up --build -d
                        '
                    """
                }
            }
        }

        stage('Health Check') {
            steps {
                sleep(time: 20, unit: 'SECONDS')
                sh "curl -f http://${SERVER_IP}:3001 || exit 1"
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