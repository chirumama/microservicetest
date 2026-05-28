pipeline {
    agent any

    environment {
        SERVER_IP = '3.110.46.238'
        APP_DIR   = '/home/ubuntu/Microservice_Dash'
        GIT_REPO  = 'https://github.com/paypoint/MicroserviceDashboard.git'
    }

    stages {

        stage('Clone / Pull') {
            steps {
                sshagent(['ubuntu-server-ssh']) {
                    sh """
                        ssh -o StrictHostKeyChecking=no ubuntu@${SERVER_IP} '
                            if [ -d "${APP_DIR}/.git" ]; then
                                cd ${APP_DIR} && git pull origin main
                            else
                                git clone ${GIT_REPO} ${APP_DIR}
                            fi
                        '
                    """
                }
            }
        }

        stage('Build & Deploy') {
            steps {
                sshagent(['ubuntu-server-ssh']) {
                    sh """
                        ssh -o StrictHostKeyChecking=no ubuntu@${SERVER_IP} '
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
                sh "curl -f http://${SERVER_IP}:5000 || exit 1"
            }
        }
    }

    post {
        success {
            echo 'Deployment successful!'
        }
        failure {
            sshagent(['ubuntu-server-ssh']) {
                sh "ssh -o StrictHostKeyChecking=no ubuntu@${SERVER_IP} 'cd ${APP_DIR} && docker compose logs --tail=50'"
            }
        }
    }
}