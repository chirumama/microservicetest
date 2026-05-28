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
                withCredentials([
                    sshUserPrivateKey(credentialsId: 'ubuntu-server-ssh', keyFileVariable: 'SSH_KEY'),
                    usernamePassword(credentialsId: 'github-creds', usernameVariable: 'GIT_USER', passwordVariable: 'GIT_TOKEN')
                ]) {
                    sh '''
                        ssh -i $SSH_KEY -o StrictHostKeyChecking=no ubuntu@''' + env.SERVER_IP + ''' "
                            if [ -d \\"''' + env.APP_DIR + '''/.git\\" ]; then
                                cd ''' + env.APP_DIR + '''
                                git pull https://''' + '$GIT_USER' + ''':''' + '$GIT_TOKEN' + '''@github.com/paypoint/MicroserviceDashboard.git main
                            else
                                git clone https://''' + '$GIT_USER' + ''':''' + '$GIT_TOKEN' + '''@github.com/paypoint/MicroserviceDashboard.git ''' + env.APP_DIR + '''
                            fi
                        "
                    '''
                }
            }
        }

        stage('Build & Deploy') {
            steps {
                withCredentials([
                    sshUserPrivateKey(credentialsId: 'ubuntu-server-ssh', keyFileVariable: 'SSH_KEY')
                ]) {
                    sh '''
                        ssh -i $SSH_KEY -o StrictHostKeyChecking=no ubuntu@''' + env.SERVER_IP + ''' "
                            cd ''' + env.APP_DIR + '''
                            docker compose down
                            docker compose up --build -d
                        "
                    '''
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